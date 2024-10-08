using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Nickel;

internal sealed class AssemblyModLoadContextProvider(
	AssemblyLoadContext? fallbackContext,
	IAssemblyEditor? assemblyEditor,
	ILogger logger
) : IAssemblyPluginLoaderLoadContextProvider<IAssemblyModManifest>
{
	private readonly ConditionalWeakTable<IPluginPackage<IAssemblyModManifest>, WeakReference<AssemblyLoadContext>> ContextCache = [];
	private readonly Dictionary<string, Assembly> AssemblyNameToSharedAssembly = [];

	public AssemblyLoadContext GetLoadContext(IPluginPackage<IAssemblyModManifest> package)
	{
		if (this.ContextCache.TryGetValue(package, out var weakContext) && weakContext.TryGetTarget(out var context))
			return context;

		context = new CustomContext(fallbackContext, assemblyEditor, logger, this.AssemblyNameToSharedAssembly, package);
		this.ContextCache.AddOrUpdate(package, new WeakReference<AssemblyLoadContext>(context));
		return context;
	}

	private sealed class CustomContext(
		AssemblyLoadContext? fallbackContext,
		IAssemblyEditor? assemblyEditor,
		ILogger logger,
		Dictionary<string, Assembly> assemblyNameToSharedAssembly,
		IPluginPackage<IAssemblyModManifest> package
	) : AssemblyLoadContext
	{
		private readonly Dictionary<string, Assembly> AssemblyCache = [];
		
		protected override Assembly? Load(AssemblyName assemblyName)
		{
			var assemblyNameString = assemblyName.Name ?? assemblyName.FullName;
			
			if (this.AssemblyCache.TryGetValue(assemblyName.Name ?? assemblyName.FullName, out var cachedAssembly))
				return cachedAssembly;

			var assembly = this.LoadUncachedAssembly(assemblyName, assemblyNameString);
			if (assembly is not null)
				this.AssemblyCache[assemblyNameString] = assembly;
			return assembly;
		}

		private Assembly? LoadUncachedAssembly(AssemblyName assemblyName, string assemblyNameString)
		{
			if (assemblyNameToSharedAssembly.TryGetValue(assemblyNameString, out var sharedAssembly))
				return sharedAssembly;
			
			if (fallbackContext?.Assemblies.FirstOrDefault(a => (a.GetName().Name ?? a.GetName().FullName) == assemblyNameString) is { } existingFallbackAssembly)
				return existingFallbackAssembly;

			var file = package.PackageRoot.GetRelativeFile($"{assemblyNameString}.dll");
			if (file.Exists)
			{
				using var originalAssemblyStream = file.OpenRead();
				var refAssemblyStream = originalAssemblyStream;
				
				if (assemblyEditor?.EditAssemblyStream(assemblyName.Name ?? assemblyName.FullName, ref refAssemblyStream) is { } editorResult)
					foreach (var message in editorResult.Messages)
						logger.Log(message.Level switch
						{
							AssemblyEditorResult.MessageLevel.Error => LogLevel.Error,
							AssemblyEditorResult.MessageLevel.Warning => LogLevel.Warning,
							AssemblyEditorResult.MessageLevel.Info => LogLevel.Information,
							AssemblyEditorResult.MessageLevel.Debug => LogLevel.Debug,
							_ => LogLevel.Error
						}, message.Content);
				
				using var modifiedAssemblyStream = refAssemblyStream;
				var assembly = this.LoadFromStream(modifiedAssemblyStream);

				if (this.ShouldShare(assemblyNameString))
					assemblyNameToSharedAssembly[assembly.GetName().Name ?? assembly.GetName().FullName] = assembly;

				return assembly;
			}

			if (fallbackContext?.LoadFromAssemblyName(assemblyName) is { } fallbackAssembly)
				return fallbackAssembly;

			return null;
		}

		private bool ShouldShare(string assemblyName)
		{
			if (package.Manifest.AssemblyReferences.FirstOrDefault(r => r.Name == assemblyName) is { } reference)
				return reference.IsShared;
			if (typeof(Nickel).Assembly.GetReferencedAssemblies().Any(a => (a.Name ?? a.FullName) == assemblyName))
				return true;
			if (package.PackageRoot.GetRelativeFile($"{assemblyName}.dll").Exists)
				return package.Manifest.EntryPointAssembly == $"{assemblyName}.dll";
			if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{assemblyName}.dll")))
				return true;
			return false;
		}
	}
}

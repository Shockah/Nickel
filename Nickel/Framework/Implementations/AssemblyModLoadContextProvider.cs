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
	AssemblyLoadContext? fallbackContext
) : IAssemblyPluginLoaderLoadContextProvider<IAssemblyModManifest>
{
	private readonly ConditionalWeakTable<IPluginPackage<IAssemblyModManifest>, WeakReference<AssemblyLoadContext>> ContextCache = [];
	private readonly Dictionary<string, WeakReference<Assembly>> AssemblyNameToSharedAssembly = [];

	public AssemblyLoadContext GetLoadContext(IPluginPackage<IAssemblyModManifest> package)
	{
		if (this.ContextCache.TryGetValue(package, out var weakContext) && weakContext.TryGetTarget(out var context))
			return context;

		context = new CustomContext(fallbackContext, this.AssemblyNameToSharedAssembly, package);
		this.ContextCache.AddOrUpdate(package, new WeakReference<AssemblyLoadContext>(context));
		return context;
	}

	private sealed class CustomContext(
		AssemblyLoadContext? fallbackContext,
		Dictionary<string, WeakReference<Assembly>> assemblyNameToSharedAssembly,
		IPluginPackage<IAssemblyModManifest> package
	) : AssemblyLoadContext
	{
		protected override Assembly? Load(AssemblyName assemblyName)
		{
			if (this.Assemblies.FirstOrDefault(a => (a.GetName().Name ?? a.GetName().FullName) == (assemblyName.Name ?? assemblyName.FullName)) is { } existingPrivateAssembly)
				return existingPrivateAssembly;

			if (fallbackContext?.Assemblies.FirstOrDefault(a => (a.GetName().Name ?? a.GetName().FullName) == (assemblyName.Name ?? assemblyName.FullName)) is { } existingFallbackAssembly)
				return existingFallbackAssembly;

			if (assemblyNameToSharedAssembly.TryGetValue(assemblyName.Name ?? assemblyName.FullName, out var weakSharedAssembly) && weakSharedAssembly.TryGetTarget(out var sharedAssembly))
				return sharedAssembly;

			var file = package.PackageRoot.GetRelativeFile($"{assemblyName.Name ?? assemblyName.FullName}.dll");
			if (file.Exists)
			{
				using var assemblyStream = package.PackageRoot.GetRelativeFile($"{assemblyName.Name ?? assemblyName.FullName}.dll").OpenRead();
				var assembly = this.LoadFromStream(assemblyStream);

				if (this.ShouldShare(assemblyName))
					assemblyNameToSharedAssembly[assembly.GetName().Name ?? assembly.GetName().FullName] = new WeakReference<Assembly>(assembly);

				return assembly;
			}

			return fallbackContext?.LoadFromAssemblyName(assemblyName);
		}

		private bool ShouldShare(AssemblyName assemblyName)
		{
			if (package.Manifest.AssemblyReferences.FirstOrDefault(r => r.Name == (assemblyName.Name ?? assemblyName.FullName)) is { } reference)
				return reference.IsShared;
			if (typeof(Nickel).Assembly.GetReferencedAssemblies().Any(a => (a.Name ?? a.FullName) == (assemblyName.Name ?? assemblyName.FullName)))
				return true;
			if (package.PackageRoot.GetRelativeFile($"{assemblyName.Name ?? assemblyName.FullName}.dll").Exists)
				return package.Manifest.EntryPointAssembly == $"{assemblyName.Name ?? assemblyName.FullName}.dll";
			if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{assemblyName.Name ?? assemblyName.FullName}.dll")))
				return true;
			return false;
		}
	}
}

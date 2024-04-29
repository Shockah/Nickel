using Nanoray.PluginManager;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Nickel;

internal sealed class AssemblyModLoadContextProvider : IAssemblyPluginLoaderLoadContextProvider<IAssemblyModManifest>
{
	private readonly AssemblyLoadContext? FallbackContext;
	private readonly ConditionalWeakTable<IPluginPackage<IAssemblyModManifest>, WeakReference<AssemblyLoadContext>> ContextCache = [];
	private readonly List<WeakReference<Assembly>> SharedAssemblies = [];

	public AssemblyModLoadContextProvider(AssemblyLoadContext? fallbackContext)
	{
		this.FallbackContext = fallbackContext;
	}

	public AssemblyLoadContext GetLoadContext(IPluginPackage<IAssemblyModManifest> package)
	{
		if (this.ContextCache.TryGetValue(package, out var weakContext) && weakContext.TryGetTarget(out var context))
			return context;

		context = new CustomContext(this.FallbackContext, this.SharedAssemblies, package);
		this.ContextCache.AddOrUpdate(package, new WeakReference<AssemblyLoadContext>(context));
		return context;
	}

	private sealed class CustomContext : AssemblyLoadContext
	{
		private readonly AssemblyLoadContext? FallbackContext;
		private readonly List<WeakReference<Assembly>> SharedAssemblies;
		private IPluginPackage<IAssemblyModManifest> Package { get; }

		public CustomContext(
			AssemblyLoadContext? fallbackContext,
			List<WeakReference<Assembly>> sharedAssemblies,
			IPluginPackage<IAssemblyModManifest> package
		) : base(package.Manifest.UniqueName)
		{
			this.FallbackContext = fallbackContext;
			this.SharedAssemblies = sharedAssemblies;
			this.Package = package;
		}

		protected override Assembly? Load(AssemblyName assemblyName)
		{
			if (this.Assemblies.FirstOrDefault(a => (a.GetName().Name ?? a.GetName().FullName) == (assemblyName.Name ?? assemblyName.FullName)) is { } existingPrivateAssembly)
				return existingPrivateAssembly;

			if (this.FallbackContext is { } fallbackContext)
				if (fallbackContext.Assemblies.FirstOrDefault(a => (a.GetName().Name ?? a.GetName().FullName) == (assemblyName.Name ?? assemblyName.FullName)) is { } existingFallbackAssembly)
					return existingFallbackAssembly;

			foreach (var weakSharedAssembly in this.SharedAssemblies)
				if (weakSharedAssembly.TryGetTarget(out var sharedAssembly) && (sharedAssembly.GetName().Name ?? sharedAssembly.GetName().FullName) == (assemblyName.Name ?? assemblyName.FullName))
					return sharedAssembly;

			var file = this.Package.PackageRoot.GetRelativeFile($"{assemblyName.Name ?? assemblyName.FullName}.dll");
			if (file.Exists)
			{
				using var assemblyStream = this.Package.PackageRoot.GetRelativeFile($"{assemblyName.Name ?? assemblyName.FullName}.dll").OpenRead();
				var assembly = this.LoadFromStream(assemblyStream);

				if (this.ShouldShare(assemblyName))
					this.SharedAssemblies.Add(new(assembly));

				return assembly;
			}

			return this.FallbackContext?.LoadFromAssemblyName(assemblyName);
		}

		private bool ShouldShare(AssemblyName assemblyName)
		{
			if (this.Package.Manifest.AssemblyReferences.FirstOrDefault(r => r.Name == (assemblyName.Name ?? assemblyName.FullName)) is { } reference)
				return reference.IsShared;
			if (typeof(Nickel).Assembly.GetReferencedAssemblies().Any(a => (a.Name ?? a.FullName) == (assemblyName.Name ?? assemblyName.FullName)))
				return true;
			if (this.Package.PackageRoot.GetRelativeFile($"{assemblyName.Name ?? assemblyName.FullName}.dll").Exists)
				return this.Package.Manifest.EntryPointAssembly == $"{assemblyName.Name ?? assemblyName.FullName}.dll";
			if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{assemblyName.Name ?? assemblyName.FullName}.dll")))
				return true;
			return false;
		}
	}
}

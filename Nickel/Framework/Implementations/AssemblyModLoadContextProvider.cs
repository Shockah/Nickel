using Nanoray.PluginManager;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Nickel;

internal sealed class AssemblyModLoadContextProvider(
	AssemblyLoadContext sharedContext
) : IAssemblyPluginLoaderLoadContextProvider<IAssemblyModManifest>
{
	private AssemblyLoadContext SharedContext { get; } = sharedContext;
	private ConditionalWeakTable<IPluginPackage<IAssemblyModManifest>, WeakReference<AssemblyLoadContext>> ContextCache { get; } = [];

	private static bool ShouldShare(IPluginPackage<IAssemblyModManifest> package, AssemblyName assemblyName)
	{
		var reference = package.Manifest.AssemblyReferences.FirstOrDefault(r => r.Name == (assemblyName.Name ?? assemblyName.FullName));
		if (reference is not null)
			return reference.IsShared;
		if (typeof(Nickel).Assembly.GetReferencedAssemblies().Any(a => (a.Name ?? a.FullName) == (assemblyName.Name ?? assemblyName.FullName)))
			return true;
		return false;
	}

	public AssemblyLoadContext GetLoadContext(IPluginPackage<IAssemblyModManifest> package)
	{
		if (this.ContextCache.TryGetValue(package, out var weakContext) && weakContext.TryGetTarget(out var context))
			return context;

		context = new CustomContext(this.SharedContext, package);
		this.ContextCache.AddOrUpdate(package, new WeakReference<AssemblyLoadContext>(context));
		return context;
	}

	private sealed class CustomContext(
		AssemblyLoadContext sharedContext,
		IPluginPackage<IAssemblyModManifest> package
	) : AssemblyLoadContext
	{
		private AssemblyLoadContext SharedContext { get; } = sharedContext;
		private IPluginPackage<IAssemblyModManifest> Package { get; } = package;

		protected override Assembly? Load(AssemblyName assemblyName)
		{
			if (this.Assemblies.FirstOrDefault(a => (a.GetName().Name ?? a.GetName().FullName) == (assemblyName.Name ?? assemblyName.FullName)) is { } existingPrivateAssembly)
				return existingPrivateAssembly;
			if (this.SharedContext.Assemblies.FirstOrDefault(a => (a.GetName().Name ?? a.GetName().FullName) == (assemblyName.Name ?? assemblyName.FullName)) is { } existingSharedAssembly)
				return existingSharedAssembly;

			if (ShouldShare(this.Package, assemblyName))
			{
				try
				{
					return this.SharedContext.LoadFromAssemblyName(assemblyName);
				}
				catch
				{
					using var assemblyStream = this.Package.PackageRoot.GetRelativeFile($"{assemblyName.Name ?? assemblyName.FullName}.dll").OpenRead();
					return this.SharedContext.LoadFromStream(assemblyStream);
				}
			}

			using var assemblyStream2 = this.Package.PackageRoot.GetRelativeFile($"{assemblyName.Name ?? assemblyName.FullName}.dll").OpenRead();
			return this.LoadFromStream(assemblyStream2);
		}
	}
}

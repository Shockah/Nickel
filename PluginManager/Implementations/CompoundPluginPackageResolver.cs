using OneOf;
using OneOf.Types;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

public sealed class CompoundPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
	private IReadOnlyList<IPluginPackageResolver<TPluginManifest>> PackageResolvers { get; }

	public CompoundPluginPackageResolver(IReadOnlyList<IPluginPackageResolver<TPluginManifest>> packageResolvers)
	{
		this.PackageResolvers = packageResolvers;
	}

	public CompoundPluginPackageResolver(params IPluginPackageResolver<TPluginManifest>[] packageResolvers) : this((IReadOnlyList<IPluginPackageResolver<TPluginManifest>>)packageResolvers) { }

	public IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages()
	{
		foreach (var resolver in this.PackageResolvers)
			foreach (var package in resolver.ResolvePluginPackages())
				yield return package;
	}
}

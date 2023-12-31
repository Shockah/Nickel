using OneOf;
using OneOf.Types;
using System.Collections.Generic;

namespace Nanoray.PluginManager.Implementations;

public sealed class InnerPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
	private IPluginPackage<TPluginManifest> OuterPackage { get; }
	private TPluginManifest InnerManifest { get; }
	private bool DisposesOuterPackage { get; }

	public InnerPluginPackageResolver(IPluginPackage<TPluginManifest> outerPackage, TPluginManifest innerManifest, bool disposesOuterPackage)
	{
		this.OuterPackage = outerPackage;
		this.InnerManifest = innerManifest;
		this.DisposesOuterPackage = disposesOuterPackage;
	}

	public IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages()
	{
		yield return new InnerPluginPackage<TPluginManifest>(this.OuterPackage, this.InnerManifest, this.DisposesOuterPackage);
	}
}

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
		yield return new InnerPluginPackage(this.OuterPackage, this.InnerManifest, this.DisposesOuterPackage);
	}

	private sealed class InnerPluginPackage : IPluginPackage<TPluginManifest>
	{
		public TPluginManifest Manifest { get; }
		public IDirectoryInfo PackageRoot { get; }

		private IPluginPackage<TPluginManifest> OuterPackage { get; }
		private bool DisposesOuterPackage { get; }

		public InnerPluginPackage(IPluginPackage<TPluginManifest> outerPackage, TPluginManifest manifest, bool disposesOuterPackage)
		{
			this.OuterPackage = outerPackage;
			this.Manifest = manifest;
			this.PackageRoot = outerPackage.PackageRoot;
			this.DisposesOuterPackage = disposesOuterPackage;
		}

		public void Dispose()
		{
			if (this.DisposesOuterPackage)
				this.OuterPackage.Dispose();
		}
	}
}

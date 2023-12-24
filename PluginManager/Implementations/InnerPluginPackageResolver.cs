using System.Collections.Generic;
using System.IO;
using OneOf;
using OneOf.Types;

namespace Nanoray.PluginManager.Implementations;

public sealed class InnerPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
    private IPluginPackage<TPluginManifest> OuterPackage { get; init; }
    private TPluginManifest InnerManifest { get; init; }

    public InnerPluginPackageResolver(IPluginPackage<TPluginManifest> outerPackage, TPluginManifest innerManifest)
    {
        this.OuterPackage = outerPackage;
        this.InnerManifest = innerManifest;
    }

    public IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages()
    {
        yield return new InnerPluginPackage(this.OuterPackage, this.InnerManifest);
    }

    private sealed class InnerPluginPackage : IPluginPackage<TPluginManifest>
    {
        public TPluginManifest Manifest { get; init; }

        public IReadOnlySet<string> DataEntries
            => this.OuterPackage.DataEntries;

        private IPluginPackage<TPluginManifest> OuterPackage { get; init; }

        public InnerPluginPackage(IPluginPackage<TPluginManifest> outerPackage, TPluginManifest manifest)
        {
            this.OuterPackage = outerPackage;
            this.Manifest = manifest;
        }

        public Stream GetDataStream(string entry)
            => this.OuterPackage.GetDataStream(entry);
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using OneOf;
using OneOf.Types;

namespace Shockah.PluginManager;

public sealed class SpecializedManifestPluginLoader<TSpecializedPluginManifest, TPluginManifest, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
    where TSpecializedPluginManifest : TPluginManifest
{
    private IPluginLoader<TSpecializedPluginManifest, TPlugin> PluginLoader { get; init; }

    public SpecializedManifestPluginLoader(IPluginLoader<TSpecializedPluginManifest, TPlugin> pluginLoader)
    {
        this.PluginLoader = pluginLoader;
    }

    public bool CanLoadPlugin(IPluginPackage<TPluginManifest> package)
        => package.Manifest is TSpecializedPluginManifest specializedManifest && this.PluginLoader.CanLoadPlugin(new SpecializedPluginPackage(package, specializedManifest));

    public OneOf<TPlugin, Error<string>> LoadPlugin(IPluginPackage<TPluginManifest> package)
    {
        if (package.Manifest is not TSpecializedPluginManifest specializedManifest)
            throw new ArgumentException($"This plugin loader cannot load the plugin package {package}.");
        SpecializedPluginPackage specializedPackage = new(package, specializedManifest);
        return this.PluginLoader.LoadPlugin(specializedPackage);
    }

    private sealed class SpecializedPluginPackage : IPluginPackage<TSpecializedPluginManifest>
    {
        public TSpecializedPluginManifest Manifest { get; init; }

        public IReadOnlySet<string> DataEntries
            => this.Package.DataEntries;

        private IPluginPackage<TPluginManifest> Package { get; init; }

        public SpecializedPluginPackage(IPluginPackage<TPluginManifest> package, TSpecializedPluginManifest manifest)
        {
            this.Package = package;
            this.Manifest = manifest;
        }

        public Stream GetDataStream(string entry)
            => this.Package.GetDataStream(entry);
    }
}

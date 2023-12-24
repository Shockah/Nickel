using System;
using System.Collections.Generic;
using System.IO;
using OneOf;
using OneOf.Types;

namespace Nanoray.PluginManager;

public sealed class SpecializedConvertingManifestPluginLoader<TSpecializedPluginManifest, TPluginManifest, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
    where TSpecializedPluginManifest : TPluginManifest
{
    private IPluginLoader<TSpecializedPluginManifest, TPlugin> PluginLoader { get; init; }
    private Func<TPluginManifest, TSpecializedPluginManifest?> Converter { get; init; }

    public SpecializedConvertingManifestPluginLoader(
        IPluginLoader<TSpecializedPluginManifest, TPlugin> pluginLoader,
        Func<TPluginManifest, TSpecializedPluginManifest?> converter
    )
    {
        this.PluginLoader = pluginLoader;
        this.Converter = converter;
    }

    public bool CanLoadPlugin(IPluginPackage<TPluginManifest> package)
    {
        var specializedManifest = this.Converter(package.Manifest);
        if (specializedManifest is null)
            return false;
        var specializedPackage = new SpecializedPluginPackage(package, specializedManifest);
        return this.PluginLoader.CanLoadPlugin(specializedPackage);
    }

    public OneOf<TPlugin, Error<string>> LoadPlugin(IPluginPackage<TPluginManifest> package)
    {
        var specializedManifest = this.Converter(package.Manifest) ?? throw new ArgumentException($"This plugin loader cannot load the plugin package {package}.");
        var specializedPackage = new SpecializedPluginPackage(package, specializedManifest);
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

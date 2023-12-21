using System;
using System.Collections.Generic;
using System.Linq;
using OneOf;
using OneOf.Types;

namespace Shockah.PluginManager;

public sealed class ExtendablePluginLoader<TPluginManifest, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
{
    private List<IPluginLoader<TPluginManifest, TPlugin>> PluginLoaders { get; init; } = new();

    public bool CanLoadPlugin(IPluginPackage<TPluginManifest> package)
        => this.PluginLoaders.Any(loader => loader.CanLoadPlugin(package));

    public OneOf<LoadedPluginInfo<TPluginManifest, TPlugin>, Error<string>> LoadPlugin(IPluginPackage<TPluginManifest> package)
    {
        foreach (var loader in this.PluginLoaders)
            if (loader.CanLoadPlugin(package))
                return loader.LoadPlugin(package);
        throw new ArgumentException($"This plugin loader cannot load the plugin package {package}.");
    }
}

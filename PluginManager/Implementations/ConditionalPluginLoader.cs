using System;
using OneOf;
using OneOf.Types;

namespace Nanoray.PluginManager;

public sealed class ConditionalPluginLoader<TPluginManifest, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
{
    private IPluginLoader<TPluginManifest, TPlugin> Loader { get; init; }
    private Func<IPluginPackage<TPluginManifest>, bool> Condition { get; init; }

    public ConditionalPluginLoader(IPluginLoader<TPluginManifest, TPlugin> loader, Func<IPluginPackage<TPluginManifest>, bool> condition)
    {
        this.Loader = loader;
        this.Condition = condition;
    }

    public bool CanLoadPlugin(IPluginPackage<TPluginManifest> package)
        => this.Condition(package) && this.Loader.CanLoadPlugin(package);

    public OneOf<TPlugin, Error<string>> LoadPlugin(IPluginPackage<TPluginManifest> package)
        => this.Loader.LoadPlugin(package);
}

namespace Shockah.PluginManager;

public record struct LoadedPluginInfo<TPluginManifest, TPlugin>(
    IPluginPackage<TPluginManifest> Package,
    TPlugin Plugin
);

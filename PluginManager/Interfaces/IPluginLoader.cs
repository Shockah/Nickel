using OneOf;
using OneOf.Types;

namespace Nanoray.PluginManager;

public interface IPluginLoader<in TPluginManifest, TPlugin>
{
    bool CanLoadPlugin(IPluginPackage<TPluginManifest> package);

    OneOf<TPlugin, Error<string>> LoadPlugin(IPluginPackage<TPluginManifest> package);
}

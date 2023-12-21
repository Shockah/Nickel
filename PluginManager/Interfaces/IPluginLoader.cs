using OneOf;
using OneOf.Types;

namespace Shockah.PluginManager;

public interface IPluginLoader<TPluginManifest, TPlugin>
{
    bool CanLoadPlugin(IPluginPackage<TPluginManifest> package);

    OneOf<LoadedPluginInfo<TPluginManifest, TPlugin>, Error<string>> LoadPlugin(IPluginPackage<TPluginManifest> package);
}

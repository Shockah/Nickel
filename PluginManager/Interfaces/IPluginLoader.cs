using OneOf;
using OneOf.Types;

namespace Nanoray.PluginManager;

public interface IPluginLoader<in TPluginManifest, TPlugin>
{
	OneOf<Yes, No, Error<string>> CanLoadPlugin(IPluginPackage<TPluginManifest> package);

	PluginLoadResult<TPlugin> LoadPlugin(IPluginPackage<TPluginManifest> package);
}

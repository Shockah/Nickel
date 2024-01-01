using OneOf;
using OneOf.Types;

namespace Nanoray.PluginManager;

public interface IPluginLoader<in TPluginManifest, TPlugin>
{
	OneOf<Yes, No, Error<string>> CanLoadPlugin(IPluginPackage<TPluginManifest> package);

	OneOf<TPlugin, Error<string>> LoadPlugin(IPluginPackage<TPluginManifest> package);
}

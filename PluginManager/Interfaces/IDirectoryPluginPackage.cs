namespace Nanoray.PluginManager;

public interface IDirectoryPluginPackage<out TPluginManifest> : IPluginPackage<TPluginManifest>
{
	IDirectoryInfo Directory { get; }
}

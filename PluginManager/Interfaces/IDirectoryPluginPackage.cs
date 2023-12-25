using System.IO;

namespace Nanoray.PluginManager;

public interface IDirectoryPluginPackage<out TPluginManifest> : IPluginPackage<TPluginManifest>
{
    DirectoryInfo Directory { get; }
}

using System.Collections.Generic;
using System.IO;

namespace Nanoray.PluginManager;

public interface IPluginPackage<out TPluginManifest>
{
    TPluginManifest Manifest { get; }
    IReadOnlySet<string> DataEntries { get; }

    Stream GetDataStream(string entry);
    string? GetDataPath(string entry) => null;
}

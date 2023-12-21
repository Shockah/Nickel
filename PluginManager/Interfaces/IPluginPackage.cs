using System.Collections.Generic;
using System.IO;

namespace Shockah.PluginManager;

public interface IPluginPackage<TPluginManifest>
{
    TPluginManifest Manifest { get; }
    IReadOnlySet<string> DataEntries { get; }

    Stream GetDataStream(string entry);
    string? GetDataPath(string entry) => null;
}

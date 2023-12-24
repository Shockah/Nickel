using System.Collections.Generic;
using Nanoray.PluginManager;

namespace Nickel;

public interface IModRegistry
{
    IReadOnlyDictionary<string, IModManifest> LoadedMods { get; }

    TApi? GetApi<TApi>(string uniqueName, SemanticVersion? minimumVersion = null) where TApi : class;
}

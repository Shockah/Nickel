using System.Collections.Generic;
using Shockah.PluginManager;

namespace Nickel;

public interface IModRegistry
{
    IReadOnlyDictionary<string, IModManifest> LoadedMods { get; }

    TApi? GetApi<TApi>(string uniqueName, SemanticVersion? minimumVersion = null) where TApi : class;
}

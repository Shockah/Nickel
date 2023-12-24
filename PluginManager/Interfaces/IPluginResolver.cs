using System.Collections.Generic;

namespace Nanoray.PluginManager;

public interface IPluginResolver<TPluginManifest>
{
    IEnumerable<TPluginManifest> ResolvePluginManifests();
}

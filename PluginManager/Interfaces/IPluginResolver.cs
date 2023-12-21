using System.Collections.Generic;

namespace Shockah.PluginManager;

public interface IPluginResolver<TPluginManifest>
{
    IEnumerable<TPluginManifest> ResolvePluginManifests();
}

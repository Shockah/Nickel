using System.Collections.Generic;

namespace Nanoray.PluginManager;

public interface IPluginResolver<out TPluginManifest>
{
	IEnumerable<TPluginManifest> ResolvePluginManifests();
}

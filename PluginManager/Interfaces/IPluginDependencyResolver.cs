using System.Collections.Generic;

namespace Shockah.PluginManager;

public interface IPluginDependencyResolver<TPluginManifest>
    where TPluginManifest : notnull
{
    PluginDependencyResolveResult<TPluginManifest> ResolveDependencies(IEnumerable<TPluginManifest> toResolve, IReadOnlySet<TPluginManifest>? resolved = null);
}

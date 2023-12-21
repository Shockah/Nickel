using System.Collections.Generic;

namespace Shockah.PluginManager;

public interface IPluginDependencyResolver<TPluginManifest>
{
    PluginDependencyResolveResult<TPluginManifest> ResolveDependencies(IEnumerable<TPluginManifest> toResolve, IReadOnlySet<TPluginManifest> resolved);
}

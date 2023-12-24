using System.Collections.Generic;

namespace Nanoray.PluginManager;

public interface IPluginDependencyResolver<TPluginManifest>
    where TPluginManifest : notnull
{
    PluginDependencyResolveResult<TPluginManifest> ResolveDependencies(IEnumerable<TPluginManifest> toResolve, IReadOnlySet<TPluginManifest>? resolved = null);
}

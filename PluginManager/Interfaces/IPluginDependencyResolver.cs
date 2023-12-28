using System;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

public interface IPluginDependencyResolver<TPluginManifest, TVersion>
    where TPluginManifest : notnull
    where TVersion : struct, IEquatable<TVersion>, IComparable<TVersion>
{
    PluginDependencyResolveResult<TPluginManifest, TVersion> ResolveDependencies(IEnumerable<TPluginManifest> toResolve, IReadOnlySet<TPluginManifest>? resolved = null);
}

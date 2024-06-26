using System;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

/// <summary>
/// A type that resolves the order plugins should be loaded, taking their dependencies into account.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TVersion">The type representing a plugin version.</typeparam>
public interface IPluginDependencyResolver<TPluginManifest, TVersion>
	where TPluginManifest : notnull
	where TVersion : struct, IEquatable<TVersion>, IComparable<TVersion>
{
	/// <summary>
	/// Resolves the order plugins should be loaded, taking their dependencies into account.
	/// </summary>
	/// <param name="toResolve">Plugins that should be resolved.</param>
	/// <param name="resolved">Plugins that were already resolved earlier, if any.</param>
	/// <returns>A resolved order of plugins to load, and any plugins that could not be resolved, along with the reasons.</returns>
	PluginDependencyResolveResult<TPluginManifest, TVersion> ResolveDependencies(IEnumerable<TPluginManifest> toResolve, IReadOnlySet<TPluginManifest>? resolved = null);
}

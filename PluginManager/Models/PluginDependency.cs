using System;

namespace Nanoray.PluginManager;

/// <summary>
/// Describes a plugin dependency.
/// </summary>
/// <param name="UniqueName">The unique name of the dependency plugin.</param>
/// <param name="Version">The minimum version of the dependency plugin, or <c>null</c> if any.</param>
/// <param name="IsRequired">Whether the dependency is required. An optional dependency affects the load order, but does not produce errors.</param>
/// <typeparam name="TVersion">The type representing a plugin version.</typeparam>
public record struct PluginDependency<TVersion>(
	string UniqueName,
	TVersion? Version = default,
	bool IsRequired = true
) where TVersion : struct, IEquatable<TVersion>, IComparable<TVersion>;

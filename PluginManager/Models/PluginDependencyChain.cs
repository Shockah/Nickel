using System;
using System.Collections.Generic;
using System.Linq;

namespace Nanoray.PluginManager;

/// <summary>
/// Describes a chain of plugins depending on each other, possibly in a cycle.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
public readonly struct PluginDependencyChain<TPluginManifest> : IEquatable<PluginDependencyChain<TPluginManifest>>
{
	/// <summary>The actual plugins taking part in the chain.</summary>
	public IReadOnlyList<TPluginManifest> Values { get; init; }

	/// <inheritdoc/>
	public bool Equals(PluginDependencyChain<TPluginManifest> other)
	{
		if (this.Values.Count != other.Values.Count)
			return false;

		for (var offset = 0; offset < this.Values.Count; offset++)
		{
			for (var i = 0; i < this.Values.Count; i++)
				if (!Equals(this.Values[i], other.Values[i + offset]))
					goto outerLoopEnd;
			return true;
			outerLoopEnd:;
		}
		return false;
	}

	/// <inheritdoc/>
	public override bool Equals(object? obj)
		=> obj is PluginDependencyChain<TPluginManifest> chain && this.Equals(chain);

	/// <inheritdoc/>
	public override int GetHashCode()
		=> this.Values.Sum(m => m?.GetHashCode() ?? 0);

	public static bool operator ==(PluginDependencyChain<TPluginManifest> left, PluginDependencyChain<TPluginManifest> right)
		=> left.Equals(right);

	public static bool operator !=(PluginDependencyChain<TPluginManifest> left, PluginDependencyChain<TPluginManifest> right)
		=> !(left == right);
}

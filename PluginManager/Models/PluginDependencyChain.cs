using System;
using System.Collections.Generic;
using System.Linq;

namespace Nanoray.PluginManager;

public readonly struct PluginDependencyChain<TPluginManifest> : IEquatable<PluginDependencyChain<TPluginManifest>>
{
	public IReadOnlyList<TPluginManifest> Values { get; init; }

	public bool Equals(PluginDependencyChain<TPluginManifest> other)
	{
		if (this.Values.Count != other.Values.Count)
			return false;

		for (int offset = 0; offset < this.Values.Count; offset++)
		{
			for (int i = 0; i < this.Values.Count; i++)
				if (!Equals(this.Values[i], other.Values[i + offset]))
					goto outerLoopEnd;
			return true;
		outerLoopEnd:;
		}
		return false;
	}

	public override bool Equals(object? obj)
		=> obj is PluginDependencyChain<TPluginManifest> chain && Equals(chain);

	public override int GetHashCode()
		=> this.Values.Sum(m => m?.GetHashCode() ?? 0);

	public static bool operator ==(PluginDependencyChain<TPluginManifest> left, PluginDependencyChain<TPluginManifest> right)
		=> left.Equals(right);

	public static bool operator !=(PluginDependencyChain<TPluginManifest> left, PluginDependencyChain<TPluginManifest> right)
		=> !(left == right);
}

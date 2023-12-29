using System;

namespace Nanoray.PluginManager;

public record struct PluginDependency<TVersion>(
	string UniqueName,
	TVersion? Version = default,
	bool IsRequired = true
) where TVersion : struct, IEquatable<TVersion>, IComparable<TVersion>;

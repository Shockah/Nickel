using System;
using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// Describes all aspects of a part type (<see cref="PType"/>).
/// </summary>
public readonly struct PartTypeConfiguration
{
	/// <summary>The artifact types exclusive to this part type.</summary>
	public IReadOnlySet<Type>? ExclusiveArtifactTypes { get; init; }
}

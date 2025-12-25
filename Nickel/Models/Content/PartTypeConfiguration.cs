using System;
using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// Describes all aspects of a part type (<see cref="PType"/>).
/// </summary>
public readonly struct PartTypeConfiguration
{
	/// <summary>A localization provider for the name of the <see cref="PType"/>.</summary>
	public SingleLocalizationProvider? Name { get; init; }

	/// <summary>A localization provider for the description of the <see cref="PType"/>.</summary>
	public SingleLocalizationProvider? Description { get; init; }

	/// <summary>The artifact types exclusive to this part type.</summary>
	public IReadOnlySet<Type>? ExclusiveArtifactTypes { get; init; }
}

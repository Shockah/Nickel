using System;

namespace Nickel;

/// <summary>
/// Describes all aspects of an <see cref="Artifact"/>.
/// </summary>
public readonly struct ArtifactConfiguration
{
	/// <summary>The <see cref="Artifact"/> subclass.</summary>
	public required Type ArtifactType { get; init; }
	
	/// <summary>The meta information regarding the <see cref="Artifact"/>.</summary>
	public required ArtifactMeta Meta { get; init; }
	
	/// <summary>The sprite of the <see cref="Artifact"/>.</summary>
	public required Spr Sprite { get; init; }
	
	/// <summary>A localization provider for the name of the <see cref="Artifact"/>.</summary>
	public SingleLocalizationProvider? Name { get; init; }
	
	/// <summary>A localization provider for the description of the <see cref="Artifact"/>.</summary>
	public SingleLocalizationProvider? Description { get; init; }
}

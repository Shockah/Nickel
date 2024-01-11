using System;

namespace Nickel;

public readonly struct ArtifactConfiguration
{
	public required Type ArtifactType { get; init; }
	public required ArtifactMeta Meta { get; init; }
	public required Spr Sprite { get; init; }
	public SingleLocalizationProvider? Name { get; init; }
	public SingleLocalizationProvider? Description { get; init; }
}

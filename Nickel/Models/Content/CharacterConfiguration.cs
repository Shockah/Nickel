using System;
using System.Collections.Generic;

namespace Nickel;

public readonly struct CharacterConfiguration
{
	public required Deck Deck { get; init; }
	public required Spr BorderSprite { get; init; }
	public required IReadOnlyList<Type> StarterCardTypes { get; init; }
	public IReadOnlyList<Type>? StarterArtifactTypes { get; init; }
	public CharacterAnimationConfiguration? NeutralAnimation { get; init; }
	public CharacterAnimationConfiguration? MiniAnimation { get; init; }
	public bool StartLocked { get; init; }
	public SingleLocalizationProvider? Description { get; init; }
}

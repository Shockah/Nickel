using System;
using System.Collections.Generic;

namespace Nickel;

public readonly struct CharacterConfiguration
{
	public required Deck Deck { get; init; }
	public required Spr BorderSprite { get; init; }
	public StarterDeck? Starters { get; init; }
	public CharacterAnimationConfiguration? NeutralAnimation { get; init; }
	public CharacterAnimationConfiguration? MiniAnimation { get; init; }
	public bool StartLocked { get; init; }
	public MissingStatusConfiguration MissingStatus { get; init; }
	public Type? ExeCardType { get; init; }
	public SingleLocalizationProvider? Description { get; init; }

	[Obsolete($"Use `{nameof(Starters)}` instead.")]
	public IReadOnlyList<Type>? StarterCardTypes { get; init; }

	[Obsolete($"Use `{nameof(Starters)}` instead.")]
	public IReadOnlyList<Type>? StarterArtifactTypes { get; init; }

	public readonly struct MissingStatusConfiguration
	{
		public Color? Color { get; init; }
		public Spr? Sprite { get; init; }
	}
}

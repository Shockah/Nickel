using System.Collections.Generic;

namespace Nickel;

public readonly struct CharacterAnimationConfiguration
{
	public required Deck Deck { get; init; }
	public required string LoopTag { get; init; }
	public required IReadOnlyList<Spr> Frames { get; init; }
}

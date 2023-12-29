using System.Collections.Generic;

namespace Nickel;

public readonly struct CharacterAnimationConfiguration
{
	public Deck Deck { get; init; }
	public string LoopTag { get; init; }
	public IReadOnlyList<Spr> Frames { get; init; }
}

using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// Describes all aspects of an animation of a <see cref="Character"/>.
/// </summary>
public readonly struct CharacterAnimationConfiguration
{
	/// <summary>The deck of the character the animation is assigned to.</summary>
	public required Deck Deck { get; init; }
	
	/// <summary>A "loop tag" of the animation. In other words, the name of the "expression".</summary>
	/// <seealso cref="Say.loopTag"/>
	public required string LoopTag { get; init; }
	
	/// <summary>The frames of the animation.</summary>
	public required IReadOnlyList<Spr> Frames { get; init; }
}

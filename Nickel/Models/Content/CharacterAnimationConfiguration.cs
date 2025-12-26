using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// Describes all aspects of an animation of a <see cref="Character"/>.
/// </summary>
public readonly struct CharacterAnimationConfiguration
{
	/// <summary>
	/// The character's type used for dialogue purposes.<br/>
	/// For playable characters, this matches their <see cref="EnumExtensions.Key(Deck)"/> (with the exception of <a href="https://cobaltcore.wiki.gg/wiki/CAT">CAT</a>, whose <see cref="CharacterType"/> is <c>comp</c>).
	/// </summary>
	public required string CharacterType { get; init; }
	
	/// <summary>A "loop tag" of the animation. In other words, the name of the "expression".</summary>
	/// <seealso cref="Say.loopTag"/>
	public required string LoopTag { get; init; }
	
	/// <summary>The frames of the animation.</summary>
	public required IReadOnlyList<Spr> Frames { get; init; }
}

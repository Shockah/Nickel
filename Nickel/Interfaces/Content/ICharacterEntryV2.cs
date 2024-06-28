namespace Nickel;

/// <summary>
/// Describes any <see cref="Character"/>, playable or not.
/// </summary>
public interface ICharacterEntryV2 : IModOwned
{
	/// <summary>
	/// The character's type used for dialogue purposes.<br/>
	/// For playable characters, this matches their <see cref="EnumExtensions.Key(Deck)"/> (with the exception of <a href="https://cobaltcore.wiki.gg/wiki/CAT">CAT</a>, whose <see cref="CharacterType"/> is <c>comp</c>).
	/// </summary>
	string CharacterType { get; }
	
	/// <summary>The custom border sprite to use for rendering the face of this <see cref="Character"/>.</summary>
	Spr? BorderSprite { get; }
}

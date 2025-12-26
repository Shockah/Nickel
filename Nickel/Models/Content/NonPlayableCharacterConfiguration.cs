namespace Nickel;

/// <summary>
/// Describes all aspects of a non-playable <see cref="Character"/> - an enemy or a story character.
/// </summary>
public readonly struct NonPlayableCharacterConfiguration
{
	/// <summary>
	/// The character's type used for dialogue purposes.<br/>
	/// For playable characters, this matches their <see cref="EnumExtensions.Key(Deck)"/> (with the exception of <a href="https://cobaltcore.wiki.gg/wiki/CAT">CAT</a>, whose <see cref="CharacterType"/> is <c>comp</c>).
	/// </summary>
	public string CharacterType { get; init; }
	
	/// <summary>The border sprite to use for rendering the face of this playable <see cref="Character"/>.</summary>
	public Spr? BorderSprite { get; init; }
	
	/// <summary>The neutral (default) animation for this character.</summary>
	/// <remarks>Either this property has to be set, or a corresponding call to <see cref="IModCharacters.RegisterCharacterAnimation(CharacterAnimationConfiguration)"/> has to be done prior to registering the character, but <b>not both</b>.</remarks>
	public CharacterAnimationConfiguration? NeutralAnimation { get; init; }
	
	/// <summary>A localization provider for the name of the <see cref="Character"/>.</summary>
	public SingleLocalizationProvider? Name { get; init; }
	
	/// <summary>Describes all aspects of a non-playable character's dialogue babble.</summary>
	public CharacterBabbleConfiguration? Babble { get; init; }

	/// <summary>
	/// Describes amends to a non-playable <see cref="Character"/>'s <see cref="NonPlayableCharacterConfiguration">configuration</see>.
	/// </summary>
	public struct Amends
	{
		/// <inheritdoc cref="NonPlayableCharacterConfiguration.Babble" />
		public ContentConfigurationValueAmend<CharacterBabbleConfiguration?>? Babble { get; set; }
	}
}

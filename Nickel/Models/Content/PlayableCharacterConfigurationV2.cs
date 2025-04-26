using System;

namespace Nickel;

/// <summary>
/// Describes all aspects of a playable <see cref="Character"/>.
/// </summary>
public readonly struct PlayableCharacterConfigurationV2
{
	/// <summary>The deck this playable <see cref="Character"/> is assigned to.</summary>
	public required Deck Deck { get; init; }
	
	/// <summary>The border sprite to use for rendering the face of this playable <see cref="Character"/>.</summary>
	public required Spr BorderSprite { get; init; }
	
	/// <summary>The cards and artifacts this playable <see cref="Character"/> starts with.</summary>
	public required StarterDeck Starters { get; init; }
	
	/// <summary>The cards and artifacts this playable <see cref="Character"/> starts with if the <see cref="DailyJustOneCharacter">Solo Run daily modifier</see> is present.</summary>
	/// <remarks>If not set, Nickel will pick common cards at random.</remarks>
	public StarterDeck? SoloStarters { get; init; }
	
	/// <summary>The neutral (default) animation for this character.</summary>
	/// <remarks>Either this property has to be set, or a corresponding call to <see cref="IModCharactersV2.RegisterCharacterAnimation(CharacterAnimationConfigurationV2)"/> has to be done prior to registering the character, but <b>not both</b>.</remarks>
	public CharacterAnimationConfigurationV2? NeutralAnimation { get; init; }
	
	/// <summary>The mini animation for this character, which appears on various non-combat screens.</summary>
	/// <remarks>Either this property has to be set, or a corresponding call to <see cref="IModCharactersV2.RegisterCharacterAnimation(CharacterAnimationConfigurationV2)"/> has to be done prior to registering the character, but <b>not both</b>.</remarks>
	public CharacterAnimationConfigurationV2? MiniAnimation { get; init; }
	
	/// <summary>Whether the playable <see cref="Character"/> should start locked.</summary>
	public bool StartLocked { get; init; }
	
	/// <summary>Describes all aspects of a playable character's <c>Character Is Missing</c> <see cref="Status"/>.</summary>
	public MissingStatusConfiguration MissingStatus { get; init; }
	
	/// <summary>The type of the card that should become this character's EXE card (see <a href="https://cobaltcore.wiki.gg/wiki/CAT">CAT</a>).</summary>
	public Type? ExeCardType { get; init; }
	
	/// <summary>A localization provider for the description of the playable <see cref="Character"/>.</summary>
	public SingleLocalizationProvider? Description { get; init; }
	
	/// <summary>Describes all aspects of a playable character's dialogue babble.</summary>
	public CharacterBabbleConfiguration? Babble { get; init; }

	/// <summary>
	/// Describes all aspects of a playable character's <c>Character Is Missing</c> <see cref="Status"/>.
	/// </summary>
	public readonly struct MissingStatusConfiguration
	{
		/// <inheritdoc cref="StatusDef.color"/>
		public Color? Color { get; init; }
		
		/// <inheritdoc cref="StatusDef.icon"/>
		public Spr? Sprite { get; init; }
	}

	/// <summary>
	/// Describes amends to a playable <see cref="Character"/>'s <see cref="PlayableCharacterConfigurationV2">configuration</see>.
	/// </summary>
	public struct Amends
	{
		/// <inheritdoc cref="PlayableCharacterConfigurationV2.SoloStarters" />
		public ContentConfigurationValueAmend<StarterDeck?>? SoloStarters { get; set; }
		
		/// <inheritdoc cref="PlayableCharacterConfigurationV2.ExeCardType" />
		public ContentConfigurationValueAmend<Type?>? ExeCardType { get; set; }
		
		/// <inheritdoc cref="PlayableCharacterConfigurationV2.Babble" />
		public ContentConfigurationValueAmend<CharacterBabbleConfiguration?>? Babble { get; set; }
	}
}

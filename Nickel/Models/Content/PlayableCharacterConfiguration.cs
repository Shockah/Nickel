using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// Describes all aspects of a playable <see cref="Character"/>.
/// </summary>
public readonly struct PlayableCharacterConfiguration
{
	/// <summary>The deck this playable <see cref="Character"/> is assigned to.</summary>
	public required Deck Deck { get; init; }
	
	/// <summary>The border sprite to use for rendering the face of this playable <see cref="Character"/>.</summary>
	public required Spr BorderSprite { get; init; }
	
	/// <summary>The artifacts this playable <see cref="Character"/> starts with.</summary>
	public List<Artifact>? StarterArtifacts { get; init; }
	
	/// <summary>The cards this playable <see cref="Character"/> starts with.</summary>
	public List<Card>? StarterCards { get; init; }
	
	/// <summary>The cards this playable <see cref="Character"/> starts with in <see cref="DailyJustOneCharacter">Solo Run dailies</see>.</summary>
	/// <remarks>If not set, Nickel will pick "sane" defaults.</remarks>
	public List<Card>? SoloStarterCards { get; init; }
	
	/// <summary>One of these cards will be guaranteed to be in your deck for this playable <see cref="Character"/> in <see cref="DailyAdjustedMindset">Adjusted Mindset dailies</see>.</summary>
	public List<Card>? AdjustedMindsetGuaranteedStarterCards { get; init; }
	
	/// <summary>The neutral (default) animation for this character.</summary>
	/// <remarks>Either this property has to be set, or a corresponding call to <see cref="IModCharacters.RegisterCharacterAnimation(CharacterAnimationConfiguration)"/> has to be done prior to registering the character, but <b>not both</b>.</remarks>
	public CharacterAnimationConfiguration? NeutralAnimation { get; init; }
	
	/// <summary>The mini animation for this character, which appears on various non-combat screens.</summary>
	/// <remarks>Either this property has to be set, or a corresponding call to <see cref="IModCharacters.RegisterCharacterAnimation(CharacterAnimationConfiguration)"/> has to be done prior to registering the character, but <b>not both</b>.</remarks>
	public CharacterAnimationConfiguration? MiniAnimation { get; init; }
	
	/// <summary>Whether the playable <see cref="Character"/> should start locked.</summary>
	public bool StartLocked { get; init; }
	
	/// <summary>Describes all aspects of a playable character's <c>Character Is Missing</c> <see cref="Status"/>.</summary>
	public MissingStatusConfiguration MissingStatus { get; init; }
	
	/// <summary>The card that should become this character's EXE card (see <a href="https://cobaltcore.wiki.gg/wiki/CAT">CAT</a>).</summary>
	public Card? ExeCard { get; init; }
	
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
	/// Describes amends to a playable <see cref="Character"/>'s <see cref="PlayableCharacterConfiguration">configuration</see>.
	/// </summary>
	public struct Amends
	{
		/// <inheritdoc cref="PlayableCharacterConfiguration.StarterArtifacts" />
		public ContentConfigurationValueAmend<List<Artifact>?>? StarterArtifacts { get; set; }
		
		/// <inheritdoc cref="PlayableCharacterConfiguration.StarterCards" />
		public ContentConfigurationValueAmend<List<Card>?>? StarterCards { get; set; }
		
		/// <inheritdoc cref="PlayableCharacterConfiguration.SoloStarterCards" />
		public ContentConfigurationValueAmend<List<Card>?>? SoloStarterCards { get; set; }
		
		/// <inheritdoc cref="PlayableCharacterConfiguration.AdjustedMindsetGuaranteedStarterCards" />
		public ContentConfigurationValueAmend<List<Card>?>? AdjustedMindsetGuaranteedStarterCards { get; set; }
		
		/// <inheritdoc cref="PlayableCharacterConfiguration.ExeCard" />
		public ContentConfigurationValueAmend<Card?>? ExeCard { get; set; }
		
		/// <inheritdoc cref="PlayableCharacterConfiguration.Babble" />
		public ContentConfigurationValueAmend<CharacterBabbleConfiguration?>? Babble { get; set; }
	}
}

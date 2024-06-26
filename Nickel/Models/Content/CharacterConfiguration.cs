using System;
using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// Describes all aspects of a playable <see cref="Character"/>.
/// </summary>
public readonly struct CharacterConfiguration
{
	/// <summary>The deck this playable <see cref="Character"/> is assigned to.</summary>
	public required Deck Deck { get; init; }
	
	/// <summary>The border sprite to use for rendering the face of this playable <see cref="Character"/>.</summary>
	public required Spr BorderSprite { get; init; }
	
	/// <summary>The cards and artifacts this playable <see cref="Character"/> starts with.</summary>
	public StarterDeck? Starters { get; init; }
	
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
	
	/// <summary>The type of the card that should become this character's EXE card (see <a href="https://cobaltcore.wiki.gg/wiki/CAT">CAT</a>).</summary>
	public Type? ExeCardType { get; init; }
	
	/// <summary>A localization provider for the description of the playable <see cref="Character"/>.</summary>
	public SingleLocalizationProvider? Description { get; init; }

	[Obsolete($"Use `{nameof(Starters)}` instead.")]
	public IReadOnlyList<Type>? StarterCardTypes { get; init; }

	[Obsolete($"Use `{nameof(Starters)}` instead.")]
	public IReadOnlyList<Type>? StarterArtifactTypes { get; init; }

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
}

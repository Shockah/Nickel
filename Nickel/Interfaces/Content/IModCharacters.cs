using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// A mod-specific character registry.
/// Allows looking up and registering playable and non-playable characters.
/// </summary>
public interface IModCharacters
{
	/// <summary>
	/// A dictionary containing all entries registered by the owner of this helper.
	/// </summary>
	IReadOnlyDictionary<string, IPlayableCharacterEntry> RegisteredPlayableCharacters { get; }
	
	/// <summary>
	/// A dictionary containing all entries registered by the owner of this helper.
	/// </summary>
	IReadOnlyDictionary<string, INonPlayableCharacterEntry> RegisteredNonPlayableCharacters { get; }
	
	/// <summary>
	/// A dictionary containing all entries registered by the owner of this helper.
	/// </summary>
	IReadOnlyDictionary<string, ICharacterAnimationEntry> RegisteredCharacterAnimations { get; }
	
	/// <summary>
	/// Registers an animation for a <see cref="Character"/>.
	/// </summary>
	/// <param name="configuration">A configuration describing all aspects of the <see cref="Character"/> animation.</param>
	/// <returns>An entry for the new <see cref="Character"/> animation.</returns>
	ICharacterAnimationEntry RegisterCharacterAnimation(CharacterAnimationConfiguration configuration);

	/// <summary>
	/// Registers an animation for a <see cref="Character"/>.
	/// </summary>
	/// <param name="name">The local (mod-level) name for the <see cref="Character"/> animation. This has to be unique across all character animations in the mod.</param>
	/// <param name="configuration">A configuration describing all aspects of the <see cref="Character"/> animation.</param>
	/// <returns>An entry for the new <see cref="Character"/> animation.</returns>
	ICharacterAnimationEntry RegisterCharacterAnimation(string name, CharacterAnimationConfiguration configuration);

	/// <summary>
	/// Lookup a playable <see cref="Character"/> entry by its <see cref="Deck"/>.
	/// </summary>
	/// <param name="deck">The deck that belongs to the playable <see cref="Character"/>.</param>
	/// <returns>An entry, or <c>null</c> if the deck does not match any known playable characters.</returns>
	IPlayableCharacterEntry? LookupByDeck(Deck deck);
	
	/// <summary>
	/// Lookup a <see cref="Character"/> entry by its <see cref="ICharacterEntry.CharacterType"/>.
	/// </summary>
	/// <param name="characterType">The character type to retrieve an entry for.</param>
	/// <returns>An entry, or <c>null</c> if the character type does not match any known characters.</returns>
	ICharacterEntry? LookupByCharacterType(string characterType);
	
	/// <summary>
	/// Lookup a <see cref="Character"/> entry by its full <see cref="IModOwned.UniqueName"/>.
	/// </summary>
	/// <param name="uniqueName">The unique name to retrieve an entry for.</param>
	/// <returns>An entry, or <c>null</c> if the unique name does not match any known characters.</returns>
	ICharacterEntry? LookupByUniqueName(string uniqueName);
	
	/// <summary>
	/// Register a new playable <see cref="Character"/>.
	/// </summary>
	/// <param name="name">The local (mod-level) name for the playable <see cref="Character"/>. This has to be unique across all characters in the mod.</param>
	/// <param name="configuration">A configuration describing all aspects of the playable <see cref="Character"/>.</param>
	/// <returns>An entry for the new playable <see cref="Character"/>.</returns>
	IPlayableCharacterEntry RegisterPlayableCharacter(string name, PlayableCharacterConfiguration configuration);
	
	/// <summary>
	/// Register a new non-playable <see cref="Character"/> - an enemy or a story character.
	/// </summary>
	/// <param name="name">The local (mod-level) name for the non-playable <see cref="Character"/>. This has to be unique across all characters in the mod.</param>
	/// <param name="configuration">A configuration describing all aspects of the non-playable <see cref="Character"/>.</param>
	/// <returns>An entry for the new non-playable <see cref="Character"/>.</returns>
	INonPlayableCharacterEntry RegisterNonPlayableCharacter(string name, NonPlayableCharacterConfiguration configuration);
}

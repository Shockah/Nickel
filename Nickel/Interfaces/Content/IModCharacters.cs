using System;

namespace Nickel;

/// <summary>
/// A mod-specific character registry.
/// Allows looking up and registering characters.
/// </summary>
public interface IModCharacters
{
	/// <summary>
	/// Access the V2 version of this interface.
	/// </summary>
	IModCharactersV2 V2 { get; }
	
	/// <summary>
	/// Registers an animation for a <see cref="Character"/>.
	/// </summary>
	/// <param name="configuration">A configuration describing all aspects of the <see cref="Character"/> animation.</param>
	/// <returns>An entry for the new <see cref="Character"/> animation.</returns>
	[Obsolete($"Use `{nameof(V2)}.{nameof(IModCharactersV2.RegisterCharacterAnimation)}` instead.")]
	ICharacterAnimationEntry RegisterCharacterAnimation(CharacterAnimationConfiguration configuration);

	/// <summary>
	/// Registers an animation for a <see cref="Character"/>.
	/// </summary>
	/// <param name="name">The local (mod-level) name for the <see cref="Character"/> animation. This has to be unique across all character animations in the mod.</param>
	/// <param name="configuration">A configuration describing all aspects of the <see cref="Character"/> animation.</param>
	/// <returns>An entry for the new <see cref="Character"/> animation.</returns>
	[Obsolete($"Use `{nameof(V2)}.{nameof(IModCharactersV2.RegisterCharacterAnimation)}` instead.")]
	ICharacterAnimationEntry RegisterCharacterAnimation(string name, CharacterAnimationConfiguration configuration);

	/// <summary>
	/// Lookup a playable <see cref="Character"/> entry by its <see cref="Deck"/>.
	/// </summary>
	/// <param name="deck">The deck that belongs to the playable <see cref="Character"/>.</param>
	/// <returns>An entry, or <c>null</c> if the deck does not match any known playable characters.</returns>
	[Obsolete($"Use `{nameof(V2)}.{nameof(IModCharactersV2.LookupByDeck)}` instead.")]
	ICharacterEntry? LookupByDeck(Deck deck);
	
	/// <summary>
	/// Lookup a playable <see cref="Character"/> entry by its full <see cref="ICharacterEntry.UniqueName"/>.
	/// </summary>
	/// <param name="uniqueName">The unique name to retrieve an entry for.</param>
	/// <returns>An entry, or <c>null</c> if the unique name does not match any known playable characters.</returns>
	[Obsolete($"Use `{nameof(V2)}.{nameof(IModCharactersV2.LookupByUniqueName)}` instead.")]
	ICharacterEntry? LookupByUniqueName(string uniqueName);
	
	/// <summary>
	/// Register a new playable <see cref="Character"/>.
	/// </summary>
	/// <param name="name">The local (mod-level) name for the playable <see cref="Character"/>. This has to be unique across all playable characters in the mod.</param>
	/// <param name="configuration">A configuration describing all aspects of the playable <see cref="Character"/>.</param>
	/// <returns>An entry for the new playable <see cref="Character"/>.</returns>
	[Obsolete($"Use `{nameof(V2)}.{nameof(IModCharactersV2.RegisterPlayableCharacter)}` instead.")]
	ICharacterEntry RegisterCharacter(string name, CharacterConfiguration configuration);
}

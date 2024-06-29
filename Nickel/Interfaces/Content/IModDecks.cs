namespace Nickel;

/// <summary>
/// A mod-specific deck registry.
/// Allows looking up and registering decks.
/// </summary>
public interface IModDecks
{
	/// <summary>
	/// Lookup a <see cref="Deck"/> entry by its enum constant.
	/// </summary>
	/// <param name="deck">The enum constant.</param>
	/// <returns>An entry, or <c>null</c> if the type does not match any known decks.</returns>
	IDeckEntry? LookupByDeck(Deck deck);
	
	/// <summary>
	/// Lookup a <see cref="Deck"/> entry by its full <see cref="IModOwned.UniqueName"/>.
	/// </summary>
	/// <param name="uniqueName">The unique name to retrieve an entry for.</param>
	/// <returns>An entry, or <c>null</c> if the unique name does not match any known decks.</returns>
	IDeckEntry? LookupByUniqueName(string uniqueName);
	
	/// <summary>
	/// Register a new <see cref="Deck"/>.
	/// </summary>
	/// <param name="name">The local (mod-level) name for the <see cref="Deck"/>. This has to be unique across all decks in the mod.</param>
	/// <param name="configuration">A configuration describing all aspects of the <see cref="Deck"/>.</param>
	/// <returns>An entry for the new <see cref="Deck"/>.</returns>
	IDeckEntry RegisterDeck(string name, DeckConfiguration configuration);
}

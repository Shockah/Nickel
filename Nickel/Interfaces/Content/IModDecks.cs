namespace Nickel;

/// <summary>
/// A mod-specific deck registry.
/// Allows looking up and registering decks.
/// </summary>
public interface IModDecks
{
	IDeckEntry? LookupByDeck(Deck deck);
	IDeckEntry? LookupByUniqueName(string uniqueName);
	IDeckEntry RegisterDeck(string name, DeckConfiguration configuration);
}

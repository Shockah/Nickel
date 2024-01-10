namespace Nickel;

public interface IModDecks
{
	IDeckEntry? LookupByDeck(Deck deck);
	IDeckEntry? LookupByUniqueName(string uniqueName);
	IDeckEntry RegisterDeck(string name, DeckConfiguration configuration);
}

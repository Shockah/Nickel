namespace Nickel;

public interface IModDecks
{
    IDeckEntry RegisterDeck(string name, DeckConfiguration configuration);
}

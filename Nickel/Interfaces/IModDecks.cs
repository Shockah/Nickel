namespace Nickel;

public interface IModDecks
{
    IDeckEntry RegisterDeck(string name, DeckDef definition);
}

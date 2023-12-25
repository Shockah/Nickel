namespace Nickel;

public interface IDeckEntry : IModOwned
{
    Deck Deck { get; }
    DeckDef Definition { get; }
}

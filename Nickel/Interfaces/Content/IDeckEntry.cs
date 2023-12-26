namespace Nickel;

public interface IDeckEntry : IModOwned
{
    Deck Deck { get; }
    DeckConfiguration Configuration { get; }
}

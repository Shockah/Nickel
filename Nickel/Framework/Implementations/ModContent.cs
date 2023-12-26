namespace Nickel;

internal sealed class ModContent : IModContent
{
    public IModSprites Sprites { get; init; }
    public IModDecks Decks { get; init; }
    public IModCards Cards { get; init; }

    public ModContent(IModSprites sprites, IModDecks decks, IModCards cards)
    {
        this.Sprites = sprites;
        this.Decks = decks;
        this.Cards = cards;
    }
}

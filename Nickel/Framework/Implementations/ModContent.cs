namespace Nickel;

internal sealed class ModContent : IModContent
{
    public IModSprites Sprites { get; init; }
    public IModDecks Decks { get; init; }

    public ModContent(IModSprites sprites, IModDecks decks)
    {
        this.Sprites = sprites;
        this.Decks = decks;
    }
}

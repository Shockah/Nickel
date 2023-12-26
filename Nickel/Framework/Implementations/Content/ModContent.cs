namespace Nickel;

internal sealed class ModContent : IModContent
{
    public IModSprites Sprites { get; init; }
    public IModDecks Decks { get; init; }
    public IModCards Cards { get; init; }
    public IModArtifacts Artifacts { get; init; }

    public ModContent(IModSprites sprites, IModDecks decks, IModCards cards, IModArtifacts artifacts)
    {
        this.Sprites = sprites;
        this.Decks = decks;
        this.Cards = cards;
        this.Artifacts = artifacts;
    }
}

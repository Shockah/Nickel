namespace Nickel;

public interface IModContent
{
    IModSprites Sprites { get; }
    IModDecks Decks { get; }
    IModCards Cards { get; }
    IModArtifacts Artifacts { get; }
}

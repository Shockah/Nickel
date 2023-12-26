using System;

namespace Nickel;

internal sealed class ContentManager
{
    public SpriteManager Sprites { get; private init; }
    public DeckManager Decks { get; private init; }
    public CardManager Cards { get; private init; }

    public ContentManager(
        SpriteManager sprites,
        DeckManager decks,
        CardManager cards
    )
    {
        this.Sprites = sprites;
        this.Decks = decks;
        this.Cards = cards;
    }

    public ContentManager(Func<ModLoadPhase> currentModLoadPhaseProvider) : this(
        new SpriteManager(),
        new DeckManager(currentModLoadPhaseProvider),
        new CardManager(currentModLoadPhaseProvider)
    ) { }
}

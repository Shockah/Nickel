using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using CobaltCoreModding.Definitions.ExternalItems;

namespace Nickel;

internal sealed class DeckManager
{
    private Func<ModLoadPhase> CurrentModLoadPhaseProvider { get; init; }

    private int NextId { get; set; } = 10_000_001;
    private Dictionary<Deck, Entry> DeckToEntry { get; init; } = new();
    private Dictionary<string, Entry> UniqueNameToEntry { get; init; } = new();
    private List<Entry> QueuedEntries { get; init; } = new();

    public DeckManager(Func<ModLoadPhase> currentModLoadPhaseProvider)
    {
        this.CurrentModLoadPhaseProvider = currentModLoadPhaseProvider;
    }

    public IDeckEntry RegisterDeck(IModManifest owner, string name, DeckConfiguration configuration)
        => this.RegisterDeckWithUniqueName(owner, null, $"{owner.UniqueName}::{name}", configuration);

    internal IDeckEntry RegisterDeckWithUniqueName(IModManifest owner, ExternalDeck? legacy, string uniqueName, DeckConfiguration configuration)
    {
        Entry entry = new(owner, legacy, uniqueName, (Deck)this.NextId++, configuration);
        this.DeckToEntry[entry.Deck] = entry;
        this.UniqueNameToEntry[entry.UniqueName] = entry;
        this.QueueOrInject(entry);
        return entry;
    }

    internal void InjectQueuedEntries()
    {
        var queued = this.QueuedEntries.ToList();
        queued.Clear();
        foreach (var entry in queued)
            this.QueueOrInject(entry);
    }

    internal bool TryGetByUniqueName(string uniqueName, [MaybeNullWhen(false)] out Entry entry)
    {
        if (this.UniqueNameToEntry.TryGetValue(uniqueName, out var typedEntry))
        {
            entry = typedEntry;
            return true;
        }
        else
        {
            entry = default;
            return false;
        }
    }

    private void QueueOrInject(Entry entry)
    {
        if (this.CurrentModLoadPhaseProvider() < ModLoadPhase.AfterDbInit)
            this.QueuedEntries.Add(entry);
        else
            Inject(entry);
    }

    private static void Inject(Entry entry)
    {
        DB.decks[entry.Deck] = entry.Configuration.Definition;
        DB.cardArtDeckDefault[entry.Deck] = entry.Configuration.DefaultCardArt;
        DB.deckBorders[entry.Deck] = entry.Configuration.BorderSprite;
        if (entry.Configuration.OverBordersSprite is { } overBordersSprite)
            DB.deckBordersOver[entry.Deck] = overBordersSprite;
    }

    internal sealed class Entry : IDeckEntry
    {
        public IModManifest ModOwner { get; init; }
        public string UniqueName { get; init; }
        public Deck Deck { get; init; }
        public DeckConfiguration Configuration { get; init; }

        internal ExternalDeck? Legacy { get; init; }

        public Entry(IModManifest modOwner, ExternalDeck? legacy, string uniqueName, Deck deck, DeckConfiguration configuration)
        {
            this.ModOwner = modOwner;
            this.Legacy = legacy;
            this.UniqueName = uniqueName;
            this.Deck = deck;
            this.Configuration = configuration;
        }
    }
}

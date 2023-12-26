using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

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
    {
        Entry entry = new(owner, $"{owner.UniqueName}::{name}", (Deck)this.NextId++, configuration);
        this.DeckToEntry[entry.Deck] = entry;
        this.UniqueNameToEntry[entry.UniqueName] = entry;
        this.QueueOrInject(entry);
        return entry;
    }

    public bool TryGetByUniqueName(string uniqueName, [MaybeNullWhen(false)] out IDeckEntry entry)
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

    internal void InjectQueuedEntries()
    {
        var queued = this.QueuedEntries.ToList();
        this.QueuedEntries.Clear();
        foreach (var entry in queued)
            this.QueueOrInject(entry);
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

    private sealed class Entry : IDeckEntry
    {
        public IModManifest ModOwner { get; init; }
        public string UniqueName { get; init; }
        public Deck Deck { get; init; }
        public DeckConfiguration Configuration { get; init; }

        public Entry(IModManifest modOwner, string uniqueName, Deck deck, DeckConfiguration configuration)
        {
            this.ModOwner = modOwner;
            this.UniqueName = uniqueName;
            this.Deck = deck;
            this.Configuration = configuration;
        }
    }
}

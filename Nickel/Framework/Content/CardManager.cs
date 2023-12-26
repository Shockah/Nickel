using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Nickel;

internal sealed class CardManager
{
    private Func<ModLoadPhase> CurrentModLoadPhaseProvider { get; init; }

    private Dictionary<Type, Entry> CardTypeToEntry { get; init; } = new();
    private Dictionary<string, Entry> UniqueNameToEntry { get; init; } = new();
    private List<Entry> QueuedEntries { get; init; } = new();

    public CardManager(Func<ModLoadPhase> currentModLoadPhaseProvider)
    {
        this.CurrentModLoadPhaseProvider = currentModLoadPhaseProvider;
    }

    public ICardEntry RegisterCard(IModManifest owner, string name, CardConfiguration configuration)
    {
        Entry entry = new(owner, $"{owner.UniqueName}::{name}", configuration);
        this.CardTypeToEntry[entry.Configuration.CardType] = entry;
        this.UniqueNameToEntry[entry.UniqueName] = entry;
        this.QueueOrInject(entry);
        return entry;
    }

    public bool TryGetByUniqueName(string uniqueName, [MaybeNullWhen(false)] out ICardEntry entry)
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
        queued.Clear();
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
        string key = entry.Configuration.CardType.Name; // TODO: change this when Card.Key gets patched
        DB.cards[key] = entry.Configuration.CardType;
        DB.cardMetas[key] = entry.Configuration.Meta;
        if (entry.Configuration.Art is { } art)
            DB.cardArt[key] = art;
    }

    private sealed class Entry : ICardEntry
    {
        public IModManifest ModOwner { get; init; }
        public string UniqueName { get; init; }
        public CardConfiguration Configuration { get; init; }

        public Entry(IModManifest modOwner, string uniqueName, CardConfiguration configuration)
        {
            this.ModOwner = modOwner;
            this.UniqueName = uniqueName;
            this.Configuration = configuration;
        }
    }
}

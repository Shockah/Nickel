using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

internal sealed class CardManager
{
    private AfterDbInitManager<Entry> Manager { get; init; }
    private Dictionary<Type, Entry> CardTypeToEntry { get; init; } = new();
    private Dictionary<string, Entry> UniqueNameToEntry { get; init; } = new();

    public CardManager(Func<ModLoadPhase> currentModLoadPhaseProvider)
    {
        this.Manager = new(currentModLoadPhaseProvider, Inject);
    }

    internal void InjectQueuedEntries()
        => this.Manager.InjectQueuedEntries();

    public ICardEntry RegisterCard(IModManifest owner, string name, CardConfiguration configuration)
    {
        Entry entry = new(owner, $"{owner.UniqueName}::{name}", configuration);
        this.CardTypeToEntry[entry.Configuration.CardType] = entry;
        this.UniqueNameToEntry[entry.UniqueName] = entry;
        this.Manager.QueueOrInject(entry);
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

    private static void Inject(Entry entry)
    {
        string key = entry.Configuration.CardType.Name; // TODO: change this when Card.Key gets patched
        DB.cards[key] = entry.Configuration.CardType;
        DB.cardMetas[key] = entry.Configuration.Meta;
        if (entry.Configuration.Art is { } art)
            DB.cardArt[key] = art;
        if (!entry.Configuration.Meta.unreleased)
            DB.releasedCards.Add((Card)Activator.CreateInstance(entry.Configuration.CardType)!);
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

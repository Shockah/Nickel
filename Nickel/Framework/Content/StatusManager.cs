using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

internal sealed class StatusManager
{
    private int NextId { get; set; } = 10_000_001;
    private AfterDbInitManager<Entry> Manager { get; init; }
    private Dictionary<Status, Entry> StatusToEntry { get; init; } = new();
    private Dictionary<string, Entry> UniqueNameToEntry { get; init; } = new();

    public StatusManager(Func<ModLoadPhase> currentModLoadPhaseProvider)
    {
        this.Manager = new(currentModLoadPhaseProvider, Inject);
    }

    internal void InjectQueuedEntries()
        => this.Manager.InjectQueuedEntries();

    public IStatusEntry RegisterStatus(IModManifest owner, string name, StatusConfiguration configuration)
    {
        Entry entry = new(owner, $"{owner.UniqueName}::{name}", (Status)this.NextId++, configuration);
        this.StatusToEntry[entry.Status] = entry;
        this.UniqueNameToEntry[entry.UniqueName] = entry;
        this.Manager.QueueOrInject(entry);
        return entry;
    }

    public bool TryGetByUniqueName(string uniqueName, [MaybeNullWhen(false)] out IStatusEntry entry)
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
        DB.statuses[entry.Status] = entry.Configuration.Definition;
    }

    private sealed class Entry : IStatusEntry
    {
        public IModManifest ModOwner { get; init; }
        public string UniqueName { get; init; }
        public Status Status { get; init; }
        public StatusConfiguration Configuration { get; init; }

        public Entry(IModManifest modOwner, string uniqueName, Status status, StatusConfiguration configuration)
        {
            this.ModOwner = modOwner;
            this.UniqueName = uniqueName;
            this.Status = status;
            this.Configuration = configuration;
        }
    }
}

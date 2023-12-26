using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Nickel;

internal sealed class ArtifactManager
{
    private Func<ModLoadPhase> CurrentModLoadPhaseProvider { get; init; }

    private Dictionary<Type, Entry> ArtifactTypeToEntry { get; init; } = new();
    private Dictionary<string, Entry> UniqueNameToEntry { get; init; } = new();
    private List<Entry> QueuedEntries { get; init; } = new();

    public ArtifactManager(Func<ModLoadPhase> currentModLoadPhaseProvider)
    {
        this.CurrentModLoadPhaseProvider = currentModLoadPhaseProvider;
    }

    public IArtifactEntry RegisterArtifact(IModManifest owner, string name, ArtifactConfiguration configuration)
    {
        Entry entry = new(owner, $"{owner.UniqueName}::{name}", configuration);
        this.ArtifactTypeToEntry[entry.Configuration.ArtifactType] = entry;
        this.UniqueNameToEntry[entry.UniqueName] = entry;
        this.QueueOrInject(entry);
        return entry;
    }

    public bool TryGetByUniqueName(string uniqueName, [MaybeNullWhen(false)] out IArtifactEntry entry)
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
        string key = entry.Configuration.ArtifactType.Name; // TODO: change this when Artifact.Key gets patched
        DB.artifacts[key] = entry.Configuration.ArtifactType;
        DB.artifactMetas[key] = entry.Configuration.Meta;
        if (entry.Configuration.Sprite is { } sprite)
            DB.artifactSprites[key] = sprite;
        if (!entry.Configuration.Meta.pools.Contains(ArtifactPool.Unreleased))
            DB.releasedArtifacts.Add(key);
    }

    private sealed class Entry : IArtifactEntry
    {
        public IModManifest ModOwner { get; init; }
        public string UniqueName { get; init; }
        public ArtifactConfiguration Configuration { get; init; }

        public Entry(IModManifest modOwner, string uniqueName, ArtifactConfiguration configuration)
        {
            this.ModOwner = modOwner;
            this.UniqueName = uniqueName;
            this.Configuration = configuration;
        }
    }
}

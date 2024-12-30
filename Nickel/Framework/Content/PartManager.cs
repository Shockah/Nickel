using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Nickel;

internal sealed class PartManager
{
	private readonly AfterDbInitManager<PartTypeEntry> PartTypeManager;
	private readonly AfterDbInitManager<PartEntry> PartInstanceManager;
	private readonly EnumCasePool EnumCasePool;
	private readonly Dictionary<string, PartTypeEntry> UniqueNameToPartTypeEntry = [];
	private readonly Dictionary<string, PartEntry> UniqueNameToPartInstanceEntry = [];

	public PartManager(EnumCasePool enumCasePool, Func<ModLoadPhaseState> currentModLoadPhaseProvider)
	{
		this.PartTypeManager = new(currentModLoadPhaseProvider, Inject);
		this.PartInstanceManager = new(currentModLoadPhaseProvider, Inject);
		this.EnumCasePool = enumCasePool;
		ArtifactRewardPatches.OnGetBlockedArtifacts += this.OnGetBlockedArtifacts;
	}

	private void OnGetBlockedArtifacts(object? _, ArtifactRewardPatches.GetBlockedArtifactsEventArgs e)
	{
		foreach (var entry in this.UniqueNameToPartTypeEntry.Values)
		{
			if (e.State.ship.parts.Any(p => p.type != entry.PartType))
				continue;
			foreach (var artifactType in entry.Configuration.ExclusiveArtifactTypes ?? Enumerable.Empty<Type>())
				e.BlockedArtifacts.Add(artifactType);
		}
	}

	internal void InjectQueuedEntries()
		=> this.PartInstanceManager.InjectQueuedEntries();

	public IPartTypeEntry RegisterPartType(IModManifest owner, string name, PartTypeConfiguration configuration)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (this.UniqueNameToPartTypeEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A part type with the unique name `{uniqueName}` is already registered", nameof(name));
		PartTypeEntry entry = new(owner, uniqueName, this.EnumCasePool.ObtainEnumCase<PType>(), configuration);
		this.UniqueNameToPartTypeEntry[entry.UniqueName] = entry;

		this.PartTypeManager.QueueOrInject(entry);
		return entry;
	}

	public IPartEntry RegisterPart(IModManifest owner, string name, PartConfiguration configuration)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (this.UniqueNameToPartInstanceEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A part with the unique name `{uniqueName}` is already registered", nameof(name));
		PartEntry entry = new(owner, uniqueName, configuration);
		this.UniqueNameToPartInstanceEntry[entry.UniqueName] = entry;

		this.PartInstanceManager.QueueOrInject(entry);
		return entry;
	}

	public bool TryGetPartTypeByUniqueName(string uniqueName, [MaybeNullWhen(false)] out IPartTypeEntry entry)
	{
		entry = null;
		return this.UniqueNameToPartTypeEntry.TryGetValue(uniqueName, out var typedEntry);
	}

	public bool TryGetPartByUniqueName(string uniqueName, [MaybeNullWhen(false)] out IPartEntry entry)
	{
		entry = null;
		return this.UniqueNameToPartInstanceEntry.TryGetValue(uniqueName, out var typedEntry);
	}

	private static void Inject(PartTypeEntry entry)
	{
	}

	private static void Inject(PartEntry entry)
	{
		DB.parts[entry.UniqueName] = entry.Configuration.Sprite;
		if (entry.Configuration.DisabledSprite is { } disabledSprite)
			DB.partsOff[entry.UniqueName] = disabledSprite;
	}

	private sealed class PartTypeEntry(IModManifest modOwner, string uniqueName, PType partType, PartTypeConfiguration configuration)
		: IPartTypeEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public PType PartType { get; } = partType;
		public PartTypeConfiguration Configuration { get; } = configuration;

		public override string ToString()
			=> this.UniqueName;

		public override int GetHashCode()
			=> this.UniqueName.GetHashCode();
	}

	private sealed class PartEntry(IModManifest modOwner, string uniqueName, PartConfiguration configuration)
		: IPartEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public PartConfiguration Configuration { get; } = configuration;

		public override string ToString()
			=> this.UniqueName;

		public override int GetHashCode()
			=> this.UniqueName.GetHashCode();
	}
}

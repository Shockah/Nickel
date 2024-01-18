using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

internal sealed class PartManager
{
	private int NextPartTypeId { get; set; } = 10_000_001;
	private AfterDbInitManager<PartTypeEntry> PartTypeManager { get; }
	private AfterDbInitManager<PartEntry> PartInstanceManager { get; }
	private Dictionary<string, PartTypeEntry> UniqueNameToPartTypeEntry { get; } = [];
	private Dictionary<string, PartEntry> UniqueNameToPartInstanceEntry { get; } = [];

	public PartManager(Func<ModLoadPhase> currentModLoadPhaseProvider)
	{
		this.PartTypeManager = new(currentModLoadPhaseProvider, Inject);
		this.PartInstanceManager = new(currentModLoadPhaseProvider, Inject);
	}

	internal void InjectQueuedEntries()
		=> this.PartInstanceManager.InjectQueuedEntries();

	public IPartTypeEntry RegisterPartType(IModManifest owner, string name, PartTypeConfiguration configuration)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (this.UniqueNameToPartTypeEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A part type with the unique name `{uniqueName}` is already registered", nameof(name));
		PartTypeEntry entry = new(owner, uniqueName, (PType)this.NextPartTypeId++, configuration);
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
		if (this.UniqueNameToPartTypeEntry.TryGetValue(uniqueName, out var typedEntry))
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

	public bool TryGetPartByUniqueName(string uniqueName, [MaybeNullWhen(false)] out IPartEntry entry)
	{
		if (this.UniqueNameToPartInstanceEntry.TryGetValue(uniqueName, out var typedEntry))
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
	}

	private sealed class PartEntry(IModManifest modOwner, string uniqueName, PartConfiguration configuration)
		: IPartEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public PartConfiguration Configuration { get; } = configuration;
	}
}

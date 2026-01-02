using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class PartManager
{
	private readonly IModManifest VanillaModManifest;
	private readonly AfterDbInitManager<PartTypeEntry> PartTypeManager;
	private readonly AfterDbInitManager<PartEntry> PartInstanceManager;
	private readonly EnumCasePool EnumCasePool;
	private readonly Dictionary<string, PartTypeEntry> UniqueNameToPartTypeEntry = [];
	private readonly Dictionary<string, PartEntry> UniqueNameToPartInstanceEntry = [];
	private readonly Dictionary<string, PType> VanillaPartTypes;

	public PartManager(EnumCasePool enumCasePool, Func<ModLoadPhaseState> currentModLoadPhaseProvider, IModManifest vanillaModManifest)
	{
		this.VanillaModManifest = vanillaModManifest;
		this.PartTypeManager = new(currentModLoadPhaseProvider, Inject);
		this.PartInstanceManager = new(currentModLoadPhaseProvider, Inject);
		this.EnumCasePool = enumCasePool;
		
		ArtifactRewardPatches.OnGetBlockedArtifacts += this.OnGetBlockedArtifacts;

		this.VanillaPartTypes = Enum.GetValues<PType>().ToDictionary(v => Enum.GetName(v)!, v => v);
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
	{
		this.PartInstanceManager.InjectQueuedEntries();
		this.PartTypeManager.InjectQueuedEntries();
	}

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

	public IPartTypeEntry? LookupPartTypeByUniqueName(string uniqueName)
	{
		if (this.UniqueNameToPartTypeEntry.TryGetValue(uniqueName, out var entry))
			return entry;
		
		if (this.VanillaPartTypes.TryGetValue(uniqueName, out var partType))
		{
			entry = new PartTypeEntry(this.VanillaModManifest, uniqueName, partType, new()
			{
				Name = _ => Loc.T($"part.{uniqueName}.name"),
				Description = _ => Loc.T($"part.{uniqueName}.desc"),
			});

			this.UniqueNameToPartTypeEntry[uniqueName] = entry;
			return entry;
		}
		
		return null;
	}

	internal void InjectLocalizations(string locale, Dictionary<string, string> localizations)
	{
		foreach (var entry in this.UniqueNameToPartTypeEntry.Values)
			InjectLocalization(locale, localizations, entry);
	}

	private static void Inject(PartTypeEntry entry)
		=> InjectLocalization(DB.currentLocale.locale, DB.currentLocale.strings, entry);

	private static void InjectLocalization(string locale, Dictionary<string, string> localizations, PartTypeEntry entry)
	{	
		var key = entry.PartType.Key();
		if (entry.Configuration.Name.Localize(locale) is { } name)
			localizations[$"part.{key}.name"] = name;
		if (entry.Configuration.Description.Localize(locale) is { } description)
			localizations[$"part.{key}.desc"] = description;
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

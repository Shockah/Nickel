using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class ShipManager
{
	private readonly IModManifest VanillaModManifest;
	private readonly AfterDbInitManager<Entry> Manager;
	private readonly Dictionary<string, Entry> UniqueNameToEntry = [];

	public ShipManager(Func<ModLoadPhaseState> currentModLoadPhaseProvider, IModManifest vanillaModManifest)
	{
		this.VanillaModManifest = vanillaModManifest;
		this.Manager = new(currentModLoadPhaseProvider, this.Inject);
		StoryVarsPatches.OnGetUnlockedShips += this.OnGetUnlockedShips;
		ArtifactRewardPatches.OnGetBlockedArtifacts += this.OnGetBlockedArtifacts;
	}

	private void OnGetUnlockedShips(object? _, HashSet<string> unlockedShips)
	{
		foreach (var (uniqueName, entry) in this.UniqueNameToEntry)
		{
			if (entry.Configuration.StartLocked)
				continue;
			unlockedShips.Add(uniqueName);
		}
	}

	private void OnGetBlockedArtifacts(object? _, ArtifactRewardPatches.GetBlockedArtifactsEventArgs e)
	{
		foreach (var entry in this.UniqueNameToEntry.Values)
		{
			if (e.State.ship.key == entry.UniqueName)
				continue;
			foreach (var artifactType in entry.Configuration.ExclusiveArtifactTypes ?? Enumerable.Empty<Type>())
				e.BlockedArtifacts.Add(artifactType);
		}
	}

	internal void InjectQueuedEntries()
		=> this.Manager.InjectQueuedEntries();

	internal void InjectLocalizations(string locale, Dictionary<string, string> localizations)
	{
		foreach (var entry in this.UniqueNameToEntry.Values)
			InjectLocalization(locale, localizations, entry);
	}

	public IShipEntry RegisterShip(IModManifest owner, string name, ShipConfiguration configuration)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (this.UniqueNameToEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A ship with the unique name `{uniqueName}` is already registered", nameof(name));
		configuration.Ship.ship.key = uniqueName;
		Entry entry = new(owner, uniqueName, configuration);

		this.UniqueNameToEntry[entry.UniqueName] = entry;
		this.Manager.QueueOrInject(entry);
		return entry;
	}

	public IShipEntry? LookupByUniqueName(string uniqueName)
		=> this.UniqueNameToEntry.GetValueOrDefault(uniqueName);

	private void Inject(Entry entry)
	{
		entry.Configuration.Ship.ship.isPlayerShip = true;
		if (entry.Configuration.UnderChassisSprite is { } underChassisSprite)
		{
			var key = $"{entry.UniqueName}::underChassis";
			DB.parts[key] = underChassisSprite;
			entry.Configuration.Ship.ship.chassisUnder = key;
		}
		if (entry.Configuration.OverChassisSprite is { } overChassisSprite)
		{
			var key = $"{entry.UniqueName}::overChassis";
			DB.parts[key] = overChassisSprite;
			entry.Configuration.Ship.ship.chassisOver = key;
		}

		var vanillaShips = StarterShip.ships
			.Where(kvp => this.LookupByUniqueName(kvp.Key) is null)
			.ToList();
		var moddedShips = StarterShip.ships
			.Append(new KeyValuePair<string, StarterShip>(entry.UniqueName, entry.Configuration.Ship))
			.Select(kvp => this.LookupByUniqueName(kvp.Key))
			.Where(e => e is not null)
			.Select(e => e!)
			.OrderBy(e => e.ModOwner == this.VanillaModManifest ? "" : e.ModOwner.UniqueName)
			.Select(e => new KeyValuePair<string, StarterShip>(e.UniqueName, e.Configuration.Ship));
		StarterShip.ships = vanillaShips.Concat(moddedShips).ToDictionary();

		InjectLocalization(DB.currentLocale.locale, DB.currentLocale.strings, entry);
	}

	private static void InjectLocalization(string locale, Dictionary<string, string> localizations, Entry entry)
	{
		if (entry.Configuration.Name.Localize(locale) is { } name)
			localizations[$"ship.{entry.UniqueName}.name"] = name;
		if (entry.Configuration.Description.Localize(locale) is { } description)
			localizations[$"ship.{entry.UniqueName}.desc"] = description;
	}

	private sealed class Entry(IModManifest modOwner, string uniqueName, ShipConfiguration configuration)
		: IShipEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public ShipConfiguration Configuration { get; } = configuration;
	}
}

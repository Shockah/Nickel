using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Nickel;

internal sealed class ShipManager
{
	private AfterDbInitManager<Entry> Manager { get; }
	private Dictionary<string, Entry> UniqueNameToEntry { get; } = [];

	public ShipManager(Func<ModLoadPhase> currentModLoadPhaseProvider)
	{
		this.Manager = new(currentModLoadPhaseProvider, Inject);
		StoryVarsPatches.OnGetUnlockedShips.Subscribe(this.OnGetUnlockedShips);
		ArtifactRewardPatches.OnGetBlockedArtifacts.Subscribe(this.OnGetBlockedArtifacts);
	}

	private void OnGetUnlockedShips(object? sender, HashSet<string> unlockedShips)
	{
		foreach (var (uniqueName, entry) in this.UniqueNameToEntry)
		{
			if (entry.Configuration.StartLocked)
				continue;
			unlockedShips.Add(uniqueName);
		}
	}

	private void OnGetBlockedArtifacts(object? sender, ArtifactRewardPatches.GetBlockedArtifactsEventArgs e)
	{
		foreach (var (uniqueName, entry) in this.UniqueNameToEntry)
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
		configuration.Ship.ship.key = uniqueName;
		Entry entry = new(owner, uniqueName, configuration);

		this.UniqueNameToEntry[entry.UniqueName] = entry;
		this.Manager.QueueOrInject(entry);
		return entry;
	}

	public bool TryGetByUniqueName(string uniqueName, [MaybeNullWhen(false)] out IShipEntry entry)
	{
		if (this.UniqueNameToEntry.TryGetValue(uniqueName, out var typedEntry))
		{
			entry = typedEntry;
			return true;
		}

		entry = default;
		return false;
	}

	private static void Inject(Entry entry)
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

		StarterShip.ships[entry.UniqueName] = entry.Configuration.Ship;
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

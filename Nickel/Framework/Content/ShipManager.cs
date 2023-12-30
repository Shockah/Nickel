using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

internal sealed class ShipManager
{
	private AfterDbInitManager<Entry> Manager { get; }
	private Dictionary<string, Entry> UniqueNameToEntry { get; } = new();

	public ShipManager(Func<ModLoadPhase> currentModLoadPhaseProvider)
	{
		this.Manager = new(currentModLoadPhaseProvider, Inject);
		StoryVarsPatches.OnGetUnlockedShips.Subscribe(this.GetUnlockedShips);
	}

	private void GetUnlockedShips(object? sender, HashSet<string> unlockedShips)
	{
		foreach (var (uniqueName, entry) in this.UniqueNameToEntry)
		{
			if (entry.Configuration.StartLocked) continue;
			unlockedShips.Add(uniqueName);
		}
	}

	internal void InjectQueuedEntries()
		=> this.Manager.InjectQueuedEntries();

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

	private static void Inject(Entry entry) => StarterShip.ships[entry.UniqueName] = entry.Configuration.Ship;

	private sealed class Entry : IShipEntry
	{
		public IModManifest ModOwner { get; }
		public string UniqueName { get; }
		public ShipConfiguration Configuration { get; }

		public Entry(IModManifest modOwner, string uniqueName, ShipConfiguration configuration)
		{
			this.ModOwner = modOwner;
			this.UniqueName = uniqueName;
			this.Configuration = configuration;
		}
	}
}

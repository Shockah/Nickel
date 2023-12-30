using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

internal sealed class StarterShipManager
{
	private AfterDbInitManager<Entry> Manager { get; }
	private Dictionary<string, Entry> UniqueNameToEntry { get; } = new();

	public StarterShipManager(Func<ModLoadPhase> currentModLoadPhaseProvider)
	{
		this.Manager = new(currentModLoadPhaseProvider, Inject);
	}

	internal void InjectQueuedEntries()
		=> this.Manager.InjectQueuedEntries();

	public IStarterShipEntry RegisterStarterShip(IModManifest owner, string name, StarterShipConfiguration configuration)
	{
		string uniqueName = $"{owner.UniqueName}::{name}";
		configuration.Ship.ship.key = uniqueName;
		Entry entry = new(owner, uniqueName, configuration);

		this.UniqueNameToEntry[entry.UniqueName] = entry;
		this.Manager.QueueOrInject(entry);
		return entry;
	}

	public bool TryGetByUniqueName(string uniqueName, [MaybeNullWhen(false)] out IStarterShipEntry entry)
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
		StarterShip.ships[entry.UniqueName] = entry.Configuration.Ship;
	}

	private sealed class Entry : IStarterShipEntry
	{
		public IModManifest ModOwner { get; }
		public string UniqueName { get; }
		public StarterShipConfiguration Configuration { get; }

		public Entry(IModManifest modOwner, string uniqueName, StarterShipConfiguration configuration)
		{
			this.ModOwner = modOwner;
			this.UniqueName = uniqueName;
			this.Configuration = configuration;
		}
	}
}

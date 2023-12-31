using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

internal sealed class PartManager
{
	private AfterDbInitManager<Entry> Manager { get; }
	private Dictionary<string, Entry> UniqueNameToEntry { get; } = [];

	public PartManager(Func<ModLoadPhase> currentModLoadPhaseProvider)
	{
		this.Manager = new(currentModLoadPhaseProvider, Inject);
	}

	public IPartEntry RegisterPart(IModManifest owner, string name, Spr part, Spr? partOff)
	{
		Entry entry = new(owner, $"{owner.UniqueName}::{name}", part, partOff);
		this.UniqueNameToEntry[entry.UniqueName] = entry;

		this.Manager.QueueOrInject(entry);
		return entry;
	}

	public bool TryGetByUniqueName(string uniqueName, [MaybeNullWhen(false)] out IPartEntry entry)
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

	private sealed class Entry(IModManifest modOwner, string uniqueName, Spr part, Spr? partOff)
		: IPartEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public Spr Sprite { get; } = part;
		public Spr? DisabledSprite { get; } = partOff;
	}

	internal void InjectQueuedEntries()
		=> this.Manager.InjectQueuedEntries();

	private static void Inject(Entry entry)
	{
		DB.parts[entry.UniqueName] = entry.Sprite;
		if (entry.DisabledSprite != null)
			DB.partsOff[entry.UniqueName] = entry.DisabledSprite.Value;
	}
}

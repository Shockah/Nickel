using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

internal sealed class DeckManager
{
	private int NextId { get; set; } = 10_000_001;
	private AfterDbInitManager<Entry> Manager { get; }
	private Dictionary<Deck, Entry> DeckToEntry { get; } = [];
	private Dictionary<string, Entry> UniqueNameToEntry { get; } = [];

	public DeckManager(Func<ModLoadPhase> currentModLoadPhaseProvider)
	{
		this.Manager = new(currentModLoadPhaseProvider, Inject);
	}

	internal void InjectQueuedEntries()
		=> this.Manager.InjectQueuedEntries();

	public IDeckEntry RegisterDeck(IModManifest owner, string name, DeckConfiguration configuration)
	{
		Entry entry = new(owner, $"{owner.UniqueName}::{name}", (Deck)this.NextId++, configuration);
		this.DeckToEntry[entry.Deck] = entry;
		this.UniqueNameToEntry[entry.UniqueName] = entry;
		this.Manager.QueueOrInject(entry);
		return entry;
	}

	public bool TryGetByUniqueName(string uniqueName, [MaybeNullWhen(false)] out IDeckEntry entry)
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
		DB.decks[entry.Deck] = entry.Configuration.Definition;
		DB.cardArtDeckDefault[entry.Deck] = entry.Configuration.DefaultCardArt;
		DB.deckBorders[entry.Deck] = entry.Configuration.BorderSprite;
		if (entry.Configuration.OverBordersSprite is { } overBordersSprite)
			DB.deckBordersOver[entry.Deck] = overBordersSprite;
	}

	private sealed class Entry : IDeckEntry
	{
		public IModManifest ModOwner { get; }
		public string UniqueName { get; }
		public Deck Deck { get; }
		public DeckConfiguration Configuration { get; }

		public Entry(IModManifest modOwner, string uniqueName, Deck deck, DeckConfiguration configuration)
		{
			this.ModOwner = modOwner;
			this.UniqueName = uniqueName;
			this.Deck = deck;
			this.Configuration = configuration;
		}
	}
}

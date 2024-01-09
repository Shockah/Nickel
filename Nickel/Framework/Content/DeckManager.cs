using Newtonsoft.Json.Serialization;
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
	private Dictionary<string, Deck> ReservedNameToDeck { get; } = [];
	private Dictionary<Deck, string> ReservedDeckToName { get; } = [];

	public DeckManager(Func<ModLoadPhase> currentModLoadPhaseProvider)
	{
		this.Manager = new(currentModLoadPhaseProvider, Inject);
	}

	internal void ModifyJsonContract(Type type, JsonContract contract)
	{
		if (type == typeof(Deck) || type == typeof(Deck?))
		{
			contract.Converter = new ModStringEnumConverter<Deck>(
				modStringToEnumProvider: s =>
				{
					if (this.UniqueNameToEntry.TryGetValue(s, out var entry))
						return entry.Deck;
					if (this.ReservedNameToDeck.TryGetValue(s, out var @enum))
						return @enum;

					@enum = (Deck)this.NextId++;
					this.ReservedNameToDeck[s] = @enum;
					this.ReservedDeckToName[@enum] = s;
					return @enum;
				},
				modEnumToStringProvider: v =>
				{
					if (this.DeckToEntry.TryGetValue(v, out var entry))
						return entry.UniqueName;
					if (this.ReservedDeckToName.TryGetValue(v, out var name))
						return name;

					name = v.ToString();
					this.ReservedNameToDeck[name] = v;
					this.ReservedDeckToName[v] = name;
					return name;
				}
			);
		}
		else if (type.IsConstructedGenericType && (type.GetGenericTypeDefinition() == typeof(IDictionary<,>) || type.GetGenericTypeDefinition() == typeof(Dictionary<,>)) && type.GetGenericArguments()[0] == typeof(Deck))
		{
			contract.Converter = new CustomDictionaryConverter<Deck>();
		}
	}

	internal void InjectQueuedEntries()
		=> this.Manager.InjectQueuedEntries();

	internal void InjectLocalizations(string locale, Dictionary<string, string> localizations)
	{
		foreach (var entry in this.UniqueNameToEntry.Values)
			InjectLocalization(locale, localizations, entry);
	}

	public IDeckEntry RegisterDeck(IModManifest owner, string name, DeckConfiguration configuration)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		var deck = this.ReservedNameToDeck.TryGetValue(uniqueName, out var reservedStatus) ? reservedStatus : (Deck)this.NextId++;
		this.ReservedNameToDeck.Remove(uniqueName);
		this.ReservedDeckToName.Remove(deck);

		Entry entry = new(owner, $"{owner.UniqueName}::{name}", deck, configuration);
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
		Colors.colorDict[entry.Deck.Key()] = entry.Configuration.Definition.color.ToInt();

		InjectLocalization(DB.currentLocale.locale, DB.currentLocale.strings, entry);
	}

	private static void InjectLocalization(string locale, Dictionary<string, string> localizations, Entry entry)
	{
		if (entry.Configuration.Name.Localize(locale) is not { } name)
			return;
		var key = entry.Deck.Key();
		localizations[$"char.{key}"] = name;
		localizations[$"char.{key}.name"] = name;
	}

	private sealed class Entry(IModManifest modOwner, string uniqueName, Deck deck, DeckConfiguration configuration)
		: IDeckEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public Deck Deck { get; } = deck;
		public DeckConfiguration Configuration { get; } = configuration;
	}
}

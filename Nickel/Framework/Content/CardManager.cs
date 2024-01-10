using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Nickel;

internal sealed class CardManager
{
	private AfterDbInitManager<Entry> Manager { get; }
	private IModManifest VanillaModManifest { get; }
	private Dictionary<Type, Entry> CardTypeToEntry { get; } = [];
	private Dictionary<string, Entry> UniqueNameToEntry { get; } = [];

	public CardManager(Func<ModLoadPhase> currentModLoadPhaseProvider, IModManifest vanillaModManifest)
	{
		this.Manager = new(currentModLoadPhaseProvider, Inject);
		this.VanillaModManifest = vanillaModManifest;
	}

	internal void InjectQueuedEntries()
		=> this.Manager.InjectQueuedEntries();

	internal void InjectLocalizations(string locale, Dictionary<string, string> localizations)
	{
		foreach (var entry in this.UniqueNameToEntry.Values)
			InjectLocalization(locale, localizations, entry);
	}

	public ICardEntry RegisterCard(IModManifest owner, string name, CardConfiguration configuration)
	{
		Entry entry = new(owner, $"{owner.UniqueName}::{name}", configuration);
		this.CardTypeToEntry[entry.Configuration.CardType] = entry;
		this.UniqueNameToEntry[entry.UniqueName] = entry;
		this.Manager.QueueOrInject(entry);
		return entry;
	}

	public ICardEntry? LookupByCardType(Type type)
	{
		if (this.CardTypeToEntry.TryGetValue(type, out var entry))
			return entry;
		if (type.Assembly != typeof(Card).Assembly)
			return null;

		return new Entry(
			modOwner: this.VanillaModManifest,
			uniqueName: type.Name,
			configuration: new()
			{
				CardType = type,
				Meta = DB.cardMetas[type.Name],
				Art = DB.cardArt.TryGetValue(type.Name, out var art) ? art : null,
				Name = locale => DB.currentLocale.strings[$"card.{type.Name}.name"]
			}
		);
	}

	public ICardEntry? LookupByUniqueName(string uniqueName)
		=> this.UniqueNameToEntry.TryGetValue(uniqueName, out var typedEntry) ? typedEntry : null;

	private static void Inject(Entry entry)
	{
		var key = entry.Configuration.CardType.Name; // TODO: change this when Card.Key gets patched
		DB.cards[key] = entry.Configuration.CardType;
		DB.cardMetas[key] = entry.Configuration.Meta;
		if (entry.Configuration.Art is { } art)
			DB.cardArt[key] = art;
		if (!entry.Configuration.Meta.unreleased)
			DB.releasedCards.Add((Card)Activator.CreateInstance(entry.Configuration.CardType)!);

		InjectLocalization(DB.currentLocale.locale, DB.currentLocale.strings, entry);
	}

	private static void InjectLocalization(string locale, Dictionary<string, string> localizations, Entry entry)
	{
		var key = entry.Configuration.CardType.Name; // TODO: change this when Card.Key gets patched
		if (entry.Configuration.Name.Localize(locale) is { } name)
			localizations[$"card.{key}.name"] = name;
	}

	private sealed class Entry(IModManifest modOwner, string uniqueName, CardConfiguration configuration)
		: ICardEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public CardConfiguration Configuration { get; } = configuration;
	}
}

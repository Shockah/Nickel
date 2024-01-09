using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

internal sealed class CardManager
{
	private AfterDbInitManager<Entry> Manager { get; }
	private Dictionary<Type, Entry> CardTypeToEntry { get; } = [];
	private Dictionary<string, Entry> UniqueNameToEntry { get; } = [];

	public CardManager(Func<ModLoadPhase> currentModLoadPhaseProvider)
	{
		this.Manager = new(currentModLoadPhaseProvider, Inject);
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

	public bool TryGetByUniqueName(string uniqueName, [MaybeNullWhen(false)] out ICardEntry entry)
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

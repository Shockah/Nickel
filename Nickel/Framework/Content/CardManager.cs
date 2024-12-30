using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Nickel;

internal sealed class CardManager
{
	private readonly AfterDbInitManager<Entry> Manager;
	private readonly Func<IModManifest, ILogger> LoggerProvider;
	private readonly IModManifest VanillaModManifest;
	private readonly Dictionary<Type, Entry> CardTypeToEntry = [];
	private readonly Dictionary<string, Entry> UniqueNameToEntry = [];

	public CardManager(Func<ModLoadPhaseState> currentModLoadPhaseProvider, Func<IModManifest, ILogger> loggerProvider, IModManifest vanillaModManifest)
	{
		this.Manager = new(currentModLoadPhaseProvider, Inject);
		this.LoggerProvider = loggerProvider;
		this.VanillaModManifest = vanillaModManifest;

		CardPatches.OnKey += this.OnKey;
	}

	internal void InjectQueuedEntries()
		=> this.Manager.InjectQueuedEntries();

	internal void InjectLocalizations(string locale, Dictionary<string, string> localizations)
	{
		foreach (var entry in this.UniqueNameToEntry.Values)
			InjectLocalization(locale, localizations, entry);
	}

	private Entry GetMatchingExistingOrCreateNewEntry(IModManifest owner, string name, CardConfiguration configuration)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (!this.UniqueNameToEntry.TryGetValue(uniqueName, out var existing))
		{
			if (this.CardTypeToEntry.ContainsKey(configuration.CardType))
				throw new ArgumentException($"A card with the type `{configuration.CardType.FullName ?? configuration.CardType.Name}` is already registered", nameof(configuration));
			return new(owner, uniqueName, configuration);
		}
		if (existing.Configuration.CardType == configuration.CardType)
		{
			this.LoggerProvider(owner).LogDebug("Re-registering card `{UniqueName}` of type `{Type}`.", uniqueName, configuration.CardType.FullName ?? configuration.CardType.Name);
			existing.Configuration = configuration;
			return existing;
		}
		throw new ArgumentException($"A card with the unique name `{uniqueName}` is already registered");
	}

	public ICardEntry RegisterCard(IModManifest owner, string name, CardConfiguration configuration)
	{
		var entry = this.GetMatchingExistingOrCreateNewEntry(owner, name, configuration);
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
				Name = _ => Loc.T($"card.{type.Name}.name")
			}
		);
	}

	public ICardEntry? LookupByUniqueName(string uniqueName)
		=> this.UniqueNameToEntry.GetValueOrDefault(uniqueName);

	private static void Inject(Entry entry)
	{
		DB.cards[entry.UniqueName] = entry.Configuration.CardType;
		DB.cardMetas[entry.UniqueName] = entry.Configuration.Meta;

		if (entry.Configuration.Art is { } art)
			DB.cardArt[entry.UniqueName] = art;
		else
			DB.cardArt.Remove(entry.UniqueName);

		DB.releasedCards.RemoveAll(c => c.GetType() == entry.Configuration.CardType);
		if (!entry.Configuration.Meta.unreleased)
			DB.releasedCards.Add((Card)Activator.CreateInstance(entry.Configuration.CardType)!);

		InjectLocalization(DB.currentLocale.locale, DB.currentLocale.strings, entry);
	}

	private static void InjectLocalization(string locale, Dictionary<string, string> localizations, Entry entry)
	{
		if (entry.Configuration.Name.Localize(locale) is { } name)
			localizations[$"card.{entry.UniqueName}.name"] = name;
	}

	private void OnKey(object? _, CardPatches.KeyEventArgs e)
	{
		var cardType = e.Card.GetType();
		if (cardType.Assembly == typeof(Card).Assembly)
			return;
		if (this.LookupByCardType(cardType) is not { } entry)
			return;
		e.Key = entry.UniqueName;
	}

	private sealed class Entry(IModManifest modOwner, string uniqueName, CardConfiguration configuration)
		: ICardEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public CardConfiguration Configuration { get; internal set; } = configuration;

		public override string ToString()
			=> this.UniqueName;

		public override int GetHashCode()
			=> this.UniqueName.GetHashCode();
	}
}

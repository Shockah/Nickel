using HarmonyLib;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nickel;

internal sealed class DeckManager
{
	private readonly AfterDbInitManager<Entry> Manager;
	private readonly EnumCasePool EnumCasePool;
	private readonly IModManifest VanillaModManifest;
	private readonly Dictionary<Deck, Entry> DeckToEntry = [];
	private readonly Dictionary<string, Entry> UniqueNameToEntry = [];
	private readonly Dictionary<string, Deck> ReservedNameToDeck = [];
	private readonly Dictionary<Deck, string> ReservedDeckToName = [];

	private readonly FieldInfo[] FieldsAllowedToHaveInvalidEntries = [
		AccessTools.DeclaredField(typeof(StoryVars), nameof(StoryVars.unlockedChars)),
		AccessTools.DeclaredField(typeof(StoryVars), nameof(StoryVars.memoryUnlockLevel)),
	];

	public DeckManager(Func<ModLoadPhaseState> currentModLoadPhaseProvider, EnumCasePool enumCasePool, IModManifest vanillaModManifest)
	{
		this.Manager = new(currentModLoadPhaseProvider, this.Inject);
		this.EnumCasePool = enumCasePool;
		this.VanillaModManifest = vanillaModManifest;

		CardPatches.OnModifyShineColor += this.OnModifyShineColor;
	}

	internal bool IsStateInvalid(State state)
	{
		var @checked = new HashSet<object>();

		return ContainsInvalidEntries(state);

		bool ContainsInvalidEntries(object? o)
		{
			if (o is null)
				return false;
			if (o is Deck deck && this.LookupByDeck(deck) is null)
				return true;
			if (o.GetType().IsPrimitive)
				return false;
			if (!@checked.Add(o))
				return false;

			return o.GetType()
				.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
				.Any(field => !this.FieldsAllowedToHaveInvalidEntries.Contains(field) && ContainsInvalidEntries(field.GetValue(o)));
		}
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

					@enum = this.EnumCasePool.ObtainEnumCase<Deck>();
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
			this.InjectLocalization(locale, localizations, entry);
	}

	public IDeckEntry RegisterDeck(IModManifest owner, string name, DeckConfiguration configuration)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (this.UniqueNameToEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A deck with the unique name `{uniqueName}` is already registered", nameof(name));
		var deck = this.ReservedNameToDeck.TryGetValue(uniqueName, out var reservedStatus) ? reservedStatus : this.EnumCasePool.ObtainEnumCase<Deck>();
		this.ReservedNameToDeck.Remove(uniqueName);
		this.ReservedDeckToName.Remove(deck);

		EnumExtensions.deckStrs[deck] = uniqueName;

		Entry entry = new(owner, $"{owner.UniqueName}::{name}", deck, configuration, this.Amend);
		this.DeckToEntry[entry.Deck] = entry;
		this.UniqueNameToEntry[entry.UniqueName] = entry;
		this.Manager.QueueOrInject(entry);
		return entry;
	}

	public IDeckEntry? LookupByDeck(Deck deck)
	{
		if (this.DeckToEntry.TryGetValue(deck, out var entry))
			return entry;
		if (!Enum.IsDefined(deck))
			return null;

		var vanillaEntry = this.CreateVanillaEntry(deck);
		this.DeckToEntry[deck] = vanillaEntry;
		this.UniqueNameToEntry[vanillaEntry.UniqueName] = vanillaEntry;
		return vanillaEntry;
	}

	public IDeckEntry? LookupByUniqueName(string uniqueName)
	{
		if (this.UniqueNameToEntry.TryGetValue(uniqueName, out var entry))
			return entry;
		if (!Enum.TryParse<Deck>(uniqueName, out var deck))
			return null;
		
		var vanillaEntry = this.CreateVanillaEntry(deck);
		this.DeckToEntry[deck] = vanillaEntry;
		this.UniqueNameToEntry[vanillaEntry.UniqueName] = vanillaEntry;
		return vanillaEntry;
	}

	private Entry CreateVanillaEntry(Deck deck)
		=> new(
			modOwner: this.VanillaModManifest,
			uniqueName: Enum.GetName(deck)!,
			deck: deck,
			configuration: new()
			{
				Definition = DB.decks[deck],
				DefaultCardArt = DB.cardArtDeckDefault.TryGetValue(deck, out var defaultCardArt) ? defaultCardArt : Enum.Parse<Spr>(deck == Deck.trash ? "cards_ColorlessTrash" : "cards_colorless"),
				BorderSprite = DB.deckBorders.TryGetValue(deck, out var borderSprite) ? borderSprite : Enum.Parse<Spr>("cardShared_border_colorless"),
				OverBordersSprite = DB.deckBordersOver.TryGetValue(deck, out var overBordersSprite) ? overBordersSprite : null,
				Name = _ => Loc.T($"char.{deck}"),
			},
			amendDelegate: (_, _) => throw new InvalidOperationException("Vanilla entries cannot be amended")
		);

	private void Inject(Entry entry)
	{
		DB.decks[entry.Deck] = entry.Configuration.Definition;
		DB.cardArtDeckDefault[entry.Deck] = entry.Configuration.DefaultCardArt;
		DB.deckBorders[entry.Deck] = entry.Configuration.BorderSprite;
		if (entry.Configuration.OverBordersSprite is { } overBordersSprite)
			DB.deckBordersOver[entry.Deck] = overBordersSprite;
		Colors.colorDict[entry.Deck.Key()] = entry.Configuration.Definition.color.ToInt();
		Colors.colorDict[entry.Deck.ToString()] = entry.Configuration.Definition.color.ToInt();

		this.InjectLocalization(DB.currentLocale.locale, DB.currentLocale.strings, entry);
	}
	
	private void Amend(Entry entry, DeckConfiguration.Amends amends)
	{
		if (!this.UniqueNameToEntry.ContainsKey(entry.UniqueName))
			throw new ArgumentException($"A deck with the unique name `{entry.UniqueName}` is not registered");

		if (amends.ShineColorOverride is { } shineColorOverride)
			entry.Configuration = entry.Configuration with { ShineColorOverride = shineColorOverride.Value };
	}

	private void InjectLocalization(string locale, Dictionary<string, string> localizations, Entry entry)
	{
		if (entry.ModOwner == this.VanillaModManifest)
			return;
		if (entry.Configuration.Name.Localize(locale) is not { } name)
			return;
		var key = entry.Deck.Key();
		localizations[$"char.{key}"] = name;
		localizations[$"char.{key}.name"] = name;
	}
	
	private void OnModifyShineColor(object? _, ref CardPatches.ModifyShineColorEventArgs e)
	{
		if (this.LookupByDeck(e.Card.GetMeta().deck) is not { } entry)
			return;
		if (entry.Configuration.ShineColorOverride is null)
			return;
		
		e.ShineColor = entry.Configuration.ShineColorOverride(new DeckConfiguration.ShineColorOverrideArgs
		{
			State = e.State,
			Card = e.Card,
			DefaultShineColor = e.ShineColor,
		});
	}

	private sealed class Entry(
		IModManifest modOwner,
		string uniqueName,
		Deck deck,
		DeckConfiguration configuration,
		Action<Entry, DeckConfiguration.Amends> amendDelegate
	) : IDeckEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public Deck Deck { get; } = deck;
		public DeckConfiguration Configuration { get; internal set; } = configuration;

		public override string ToString()
			=> this.UniqueName;

		public override int GetHashCode()
			=> this.UniqueName.GetHashCode();
		
		public void Amend(DeckConfiguration.Amends amends)
			=> amendDelegate(this, amends);
	}
}

using Nickel.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Nickel;

internal class CardTraitManager
{
	private class Entry(IModManifest modOwner, string uniqueName, CardTraitConfiguration configuration)
		: ICardTraitEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public CardTraitConfiguration Configuration { get; } = configuration;
	}

	private class VanillaEntry
	{
		public required Entry data;
		public required Func<CardData, bool> getValue;
		public required Action<Card, bool> setOverride;
		public required Action<Card> setPermanent;
	}

	private readonly Dictionary<string, Entry> UniqueNameToEntry = [];
	private readonly Lazy<IReadOnlyDictionary<string, VanillaEntry>> SynthesizedVanillaEntries;
	private readonly IModManifest VanillaModManifest;
	private readonly IModManifest ModManagerModManifest;
	private readonly ModDataManager ModDataManager;

	public CardTraitManager(IModManifest vanillaModManifest, IModManifest modManagerModManifest, ModDataManager modDataManager)
	{
		this.VanillaModManifest = vanillaModManifest;
		this.ModManagerModManifest = modManagerModManifest;
		this.ModDataManager = modDataManager;
		this.SynthesizedVanillaEntries = new(this.SynthesizeVanillaEntries);
		CardPatches.OnGetTooltips.Subscribe(this, this.OnGetCardTooltips);
		CardPatches.OnRenderTraits.Subscribe(this, this.OnRenderTraits);
	}

	private IEnumerable<ICardTraitEntry> GetCustomTraitEntriesFor(Card card, State state) =>
		/* we intentionally skip unregistered entries here, in case a mod (safely) disappears in the middle of a run */
		this.GetAllCustomTraitsFor(card, state).Select(this.LookupByUniqueName).OfType<ICardTraitEntry>();

	private void OnRenderTraits(object? sender, CardPatches.TraitRenderEventArgs e)
	{
		var index = e.CardTraitIndex;
		foreach(var traitEntry in this.GetCustomTraitEntriesFor(e.Card, e.State)) {
			Draw.Sprite(traitEntry.Configuration.IconProvider(e.State, e.Card), e.Position.x, e.Position.y - 8 * index++);
		}
		e.CardTraitIndex = index;
	}

	private void OnGetCardTooltips(object? sender, CardPatches.TooltipsEventArgs e) =>
		e.TooltipsEnumerator = e.TooltipsEnumerator.Concat(
			this.GetCustomTraitEntriesFor(e.Card, e.State)
			.Select(x => x.Configuration.TooltipProvider?.Invoke(e.State, e.Card))
			.OfType<Tooltip>()
		);

	public ICardTraitEntry? LookupByUniqueName(string uniqueName)
	{
		if (this.UniqueNameToEntry.TryGetValue(uniqueName, out var entry))
			return entry;
		if (this.SynthesizedVanillaEntries.Value.TryGetValue(uniqueName, out var vanillaEntry))
			return vanillaEntry.data;
		return null;
	}

	public ICardTraitEntry RegisterTrait(IModManifest owner, string name, CardTraitConfiguration configuration)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (this.UniqueNameToEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A deck with the unique name `{uniqueName}` is already registered", nameof(name));
		var entry = new Entry(owner, uniqueName, configuration);
		this.UniqueNameToEntry.Add(uniqueName, entry);
		return entry;
	}

	private Dictionary<string, VanillaEntry> SynthesizeVanillaEntries()
	{
		KeyValuePair<string, VanillaEntry> SynthesizeEntry(string name, Func<CardData, bool> getValue, Action<Card, bool> setOverride, Action<Card> setPermanent)
		{
			return new KeyValuePair<string, VanillaEntry>(name, new VanillaEntry {
				data = new Entry(
					this.VanillaModManifest,
					name,
					new CardTraitConfiguration { IconProvider = (_, _) => Enum.Parse<Spr>("icons_" + name), Name = null, TooltipProvider = null }
				),
				getValue = getValue,
				setOverride = setOverride,
				setPermanent = setPermanent
			});
		}

		return new Dictionary<string, VanillaEntry>([
			SynthesizeEntry("temporary", d => d.temporary, (c, v) => c.temporaryOverride = v, c => throw new NotSupportedException("This trait cannot be permanent, since it is temporary")),
			SynthesizeEntry("exhaust", d => d.exhaust, (c, v) => c.exhaustOverride = v, c => c.exhaustOverrideIsPermanent = true),
			SynthesizeEntry("singleUse", d => d.singleUse, (c, v) => c.singleUseOverride = v, c => { /* This trait is always permanent. */ }),
			SynthesizeEntry("retain", d => d.retain, (c, v) => c.retainOverride = v, c => c.retainOverrideIsPermanent = true),
			SynthesizeEntry("buoyant", d => d.buoyant, (c, v) => c.buoyantOverride = v, c => c.buoyantOverrideIsPermanent = true),
			SynthesizeEntry("recycle", d => d.recycle, (c, v) => c.recycleOverride = v, c => c.recycleOverrideIsPermanent = true),
			SynthesizeEntry("unplayable", d => d.unplayable, (c, v) => c.unplayableOverride = v, c => c.unplayableOverrideIsPermanent = true),
		]);
	}

	private Dictionary<string, TraitOverrideEntry> GetOverrideEntriesFor(Card card) =>
		this.ModDataManager.ObtainModData(this.ModManagerModManifest, card, "CustomTraitOverrides", () => new Dictionary<string, TraitOverrideEntry>());

	public bool GetCardHasTrait(State state, Card card, string uniqueName)
	{
		if (this.SynthesizedVanillaEntries.Value.TryGetValue(uniqueName, out var vanillaEntry))
			return vanillaEntry.getValue(card.GetDataWithOverrides(state));

		var overrides = this.GetOverrideEntriesFor(card);
		if (overrides.TryGetValue(uniqueName, out var entry))
			return entry.overrideValue;

		if (card is IHasCustomCardTraits hasCustomTraits)
			return hasCustomTraits.GetInnateTraits(state).Contains(uniqueName);

		return false;
	}

	public IReadOnlySet<string> GetCardCurrentTraits(State state, Card card)
	{
		var currentTraits = card is IHasCustomCardTraits hasCustomTraits ?
			hasCustomTraits.GetInnateTraits(state).ToHashSet() :
			[]
		;

		foreach(var (uniqueName, entry) in this.GetOverrideEntriesFor(card))
		{
			if(entry.overrideValue)
				currentTraits.Add(uniqueName);
			else
				currentTraits.Remove(uniqueName);
		}

		var cardData = card.GetDataWithOverrides(state);
		foreach(var (key, value) in this.SynthesizedVanillaEntries.Value)
		{
			if(value.getValue(cardData))
				currentTraits.Add(key);
			else
				currentTraits.Remove(key);
		}

		return currentTraits;
	}

	public void AddCardTraitOverride(Card card, string uniqueName, bool overrideValue, bool isPermanent)
	{
		if (this.SynthesizedVanillaEntries.Value.TryGetValue(uniqueName, out var vanillaEntry))
		{
			vanillaEntry.setOverride(card, overrideValue);
			if (isPermanent)
				vanillaEntry.setPermanent(card);
			return;
		}

		var overrides = this.GetOverrideEntriesFor(card);
		if (overrides.TryGetValue(uniqueName, out var existingEntry))
		{
			/* mirror vanilla behavior */
			if (existingEntry.overrideIsPermanent)
				isPermanent = true;
		}

		overrides[uniqueName] = new TraitOverrideEntry { overrideValue = overrideValue, overrideIsPermanent = isPermanent };
	}

	private HashSet<string> GetAllCustomTraitsFor(Card card, State state)
	{
		HashSet<string> customTraits;
		if (card is IHasCustomCardTraits hasCustomTraits)
			customTraits = [..hasCustomTraits.GetInnateTraits(state)];
		else
			customTraits = [];

		foreach(var (uniqueName, entry) in this.GetOverrideEntriesFor(card))
		{
			if (entry.overrideValue)
				customTraits.Add(uniqueName);
			else
				customTraits.Remove(uniqueName);
		}

		return customTraits;
	}

	internal struct TraitOverrideEntry
	{
		public required bool overrideValue;
		public required bool overrideIsPermanent;
	}
}

using Nickel.Framework;
using System;
using System.Collections.Generic;
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
		public required Entry Data;
		public required Func<CardData, bool> GetValue;
		public required Action<Card, bool> SetOverride;
		public required Action<Card, bool> SetPermanent;
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
		CombatPatches.OnReturnCardsToDeck.Subscribe(this, this.OnReturnCardsToDeck);
	}

	private void OnRenderTraits(object? sender, CardPatches.TraitRenderEventArgs e)
	{
		foreach (var traitEntry in this.GetAllCustomTraitsFor(e.Card, e.State))
			if (traitEntry.Configuration.Icon(e.State, e.Card) is { } icon)
				Draw.Sprite(icon, e.Position.x, e.Position.y - 8 * e.CardTraitIndex++);
	}

	private void OnGetCardTooltips(object? sender, CardPatches.TooltipsEventArgs e)
		=> e.TooltipsEnumerator = e.TooltipsEnumerator.Concat(
			this.GetAllCustomTraitsFor(e.Card, e.State)
				.SelectMany(t => t.Configuration.Tooltips?.Invoke(e.State, e.Card) ?? [])
		);

	private void OnReturnCardsToDeck(object? sender, CombatPatches.ReturnCardsToDeckEventArgs e)
	{
		foreach (var card in e.State.deck)
		{
			if (!this.ModDataManager.TryGetModData<Dictionary<string, TraitOverrideEntry>>(this.ModManagerModManifest, card, "CustomTraitOverrides", out var overrides))
				continue;

			var toRemove = overrides.Where(x => !x.Value.OverrideIsPermanent).Select(x => x.Key).ToList();
			foreach (var key in toRemove)
				overrides.Remove(key);
		}
	}

	public ICardTraitEntry? LookupByUniqueName(string uniqueName)
	{
		if (this.UniqueNameToEntry.TryGetValue(uniqueName, out var entry))
			return entry;
		if (this.SynthesizedVanillaEntries.Value.TryGetValue(uniqueName, out var vanillaEntry))
			return vanillaEntry.Data;
		return null;
	}

	public ICardTraitEntry RegisterTrait(IModManifest owner, string name, CardTraitConfiguration configuration)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (this.UniqueNameToEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A card trait with the unique name `{uniqueName}` is already registered", nameof(name));
		var entry = new Entry(owner, uniqueName, configuration);
		this.UniqueNameToEntry.Add(uniqueName, entry);
		return entry;
	}

	private Dictionary<string, VanillaEntry> SynthesizeVanillaEntries()
	{
		KeyValuePair<string, VanillaEntry> SynthesizeEntry(string name, Func<CardData, bool> getValue, Action<Card, bool> setOverride, Action<Card, bool> setPermanent)
		{
			return new KeyValuePair<string, VanillaEntry>(name, new VanillaEntry
			{
				Data = new Entry(
					this.VanillaModManifest,
					name,
					new CardTraitConfiguration
					{
						Icon = (_, _) => Enum.TryParse<Spr>($"icons_{name}", out var icon) ? icon : null,
						Name = (_) => Loc.T($"cardtrait.{name}.name"),
						Tooltips = (_, _) => [new TTGlossary($"cardtrait.{name}")]
					}
				),
				GetValue = getValue,
				SetOverride = setOverride,
				SetPermanent = setPermanent
			});
		}

		return new Dictionary<string, VanillaEntry>([
			SynthesizeEntry("exhaust", d => d.exhaust, (c, v) => c.exhaustOverride = v, (c, v) => c.exhaustOverrideIsPermanent = true),
			SynthesizeEntry("retain", d => d.retain, (c, v) => c.retainOverride = v, (c, v) => c.retainOverrideIsPermanent = v),
			SynthesizeEntry("buoyant", d => d.buoyant, (c, v) => c.buoyantOverride = v, (c, v) => c.buoyantOverrideIsPermanent = v),
			SynthesizeEntry("recycle", d => d.recycle, (c, v) => c.recycleOverride = v, (c, v) => c.recycleOverrideIsPermanent = v),
			SynthesizeEntry("unplayable", d => d.unplayable, (c, v) => c.unplayableOverride = v, (c, v) => c.unplayableOverrideIsPermanent = v),

			SynthesizeEntry("temporary", d => d.temporary, (c, v) => c.temporaryOverride = v, (c, v) =>
			{
				if (v)
					throw new NotSupportedException("This trait cannot be made permanent");
			}),
			SynthesizeEntry("singleUse", d => d.singleUse, (c, v) => c.singleUseOverride = v, (c, v) =>
			{
				if (!v)
					throw new NotSupportedException("This trait cannot be made non-permanent");
			}),
		]);
	}

	private Dictionary<string, TraitOverrideEntry> GetOverrideEntriesFor(Card card) =>
		this.ModDataManager.ObtainModData(this.ModManagerModManifest, card, "CustomTraitOverrides", () => new Dictionary<string, TraitOverrideEntry>());

	public bool GetCardHasTrait(State state, Card card, string uniqueName)
	{
		if (this.SynthesizedVanillaEntries.Value.TryGetValue(uniqueName, out var vanillaEntry))
			return vanillaEntry.GetValue(card.GetDataWithOverrides(state));

		var overrides = this.GetOverrideEntriesFor(card);
		if (overrides.TryGetValue(uniqueName, out var entry))
			return entry.OverrideValue;

		if (card is IHasCustomCardTraits hasCustomTraits)
			return hasCustomTraits.GetInnateTraits(state).Any(t => t.UniqueName == uniqueName);

		return false;
	}

	public IReadOnlySet<ICardTraitEntry> GetCardCurrentTraits(State state, Card card)
	{
		var currentTraits = (card as IHasCustomCardTraits)?.GetInnateTraits(state).ToHashSet() ?? [];

		foreach (var (uniqueName, entry) in this.GetOverrideEntriesFor(card))
		{
			if (this.LookupByUniqueName(uniqueName) is not { } trait)
				continue;

			if (entry.OverrideValue)
				currentTraits.Add(trait);
			else
				currentTraits.Remove(trait);
		}

		var cardData = card.GetDataWithOverrides(state);
		foreach (var (key, value) in this.SynthesizedVanillaEntries.Value)
		{
			if (this.LookupByUniqueName(key) is not { } trait)
				continue;

			if (value.GetValue(cardData))
				currentTraits.Add(trait);
			else
				currentTraits.Remove(trait);
		}

		return currentTraits;
	}

	public void AddCardTraitOverride(Card card, string uniqueName, bool overrideValue, bool isPermanent)
	{
		if (this.SynthesizedVanillaEntries.Value.TryGetValue(uniqueName, out var vanillaEntry))
		{
			vanillaEntry.SetOverride(card, overrideValue);
			vanillaEntry.SetPermanent(card, isPermanent);
			return;
		}

		var overrides = this.GetOverrideEntriesFor(card);
		if (overrides.TryGetValue(uniqueName, out var existingEntry))
		{
			/* mirror vanilla behavior */
			if (existingEntry.OverrideIsPermanent)
				isPermanent = true;
		}

		overrides[uniqueName] = new TraitOverrideEntry { OverrideValue = overrideValue, OverrideIsPermanent = isPermanent };
	}

	private HashSet<ICardTraitEntry> GetAllCustomTraitsFor(Card card, State state)
	{
		var customTraits = (card as IHasCustomCardTraits)?.GetInnateTraits(state).ToHashSet() ?? [];

		foreach (var (uniqueName, entry) in this.GetOverrideEntriesFor(card))
		{
			if (this.LookupByUniqueName(uniqueName) is not { } trait)
				continue;

			if (entry.OverrideValue)
				customTraits.Add(trait);
			else
				customTraits.Remove(trait);
		}

		return customTraits;
	}

	internal struct TraitOverrideEntry
	{
		public required bool OverrideValue;
		public required bool OverrideIsPermanent;
	}
}

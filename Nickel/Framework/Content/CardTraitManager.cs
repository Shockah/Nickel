using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nickel.Models.Content;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Nickel;

internal class CardTraitManager
{
	private interface IReadWriteCardTraitEntry : ICardTraitEntry
	{
		bool IsInnatelyActive(Card card, CardData data, HashSet<ICardTraitEntry> innateCustomTraits);

		bool? GetPermanentOverride(Card card, OverridesModData overrides)
			=> overrides.Permanent.TryGetValue(this.UniqueName, out var overrideValue) ? overrideValue : null;

		bool? GetTemporaryOverride(Card card, OverridesModData overrides)
			=> overrides.Temporary.TryGetValue(this.UniqueName, out var overrideValue) ? overrideValue : null;

		void SetPermanentOverride(Card card, OverridesModData overrides, bool? overrideValue);
		void SetTemporaryOverride(Card card, OverridesModData overrides, bool? overrideValue);

		void FixModData(Card card, OverridesModData overrides) { }
	}

	private sealed class ModdedEntry(IModManifest modOwner, string uniqueName, CardTraitConfiguration configuration)
		: IReadWriteCardTraitEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public CardTraitConfiguration Configuration { get; } = configuration;

		public override string ToString()
			=> this.UniqueName;

		public bool IsInnatelyActive(Card card, CardData data, HashSet<ICardTraitEntry> innateCustomTraits)
			=> innateCustomTraits.Contains(this);

		public void SetPermanentOverride(Card card, OverridesModData overrides, bool? overrideValue)
		{
			if (overrideValue is { } newOverrideValue)
				overrides.Permanent[this.UniqueName] = newOverrideValue;
			else
				overrides.Permanent.Remove(this.UniqueName);
		}

		public void SetTemporaryOverride(Card card, OverridesModData overrides, bool? overrideValue)
		{
			if (overrideValue is { } newOverrideValue)
				overrides.Temporary[this.UniqueName] = newOverrideValue;
			else
				overrides.Temporary.Remove(this.UniqueName);
		}
	}

	private abstract class VanillaEntry(IModManifest modOwner, string dataFieldName)
		: IReadWriteCardTraitEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = dataFieldName;
		public CardTraitConfiguration Configuration { get; } = new CardTraitConfiguration
		{
			Icon = (_, _) => Enum.TryParse<Spr>($"icons_{dataFieldName}", out var icon) ? icon : null,
			Name = _ => Loc.T($"cardtrait.{dataFieldName}.name"),
			Tooltips = (_, _) => [new TTGlossary($"cardtrait.{dataFieldName}")]
		};

		private readonly Lazy<Func<CardData, bool>> GetDataValue = new(() => AccessTools.DeclaredField(typeof(CardData), dataFieldName).EmitInstanceGetter<CardData, bool>());

		public override string ToString()
			=> this.UniqueName;

		public bool IsInnatelyActive(Card card, CardData data, HashSet<ICardTraitEntry> innateCustomTraits)
			=> this.GetDataValue.Value(data);

		public virtual void SetPermanentOverride(Card card, OverridesModData overrides, bool? overrideValue)
		{
			if (overrideValue is { } newOverrideValue)
				overrides.Permanent[this.UniqueName] = newOverrideValue;
			else
				overrides.Permanent.Remove(this.UniqueName);
		}

		public virtual void SetTemporaryOverride(Card card, OverridesModData overrides, bool? overrideValue)
		{
			if (overrideValue is { } newOverrideValue)
				overrides.Temporary[this.UniqueName] = newOverrideValue;
			else
				overrides.Temporary.Remove(this.UniqueName);
		}

		public virtual void FixModData(Card card, OverridesModData overrides) { }
	}

	private class VariablePermanenceVanillaEntry(
		IModManifest modOwner,
		string dataFieldName,
		string cardOverrideValueFieldName,
		string cardOverridePermanentFieldName
	) : VanillaEntry(modOwner, dataFieldName)
	{
		private readonly Lazy<Func<Card, bool?>> GetOverrideValue = new(() => AccessTools.DeclaredField(typeof(Card), cardOverrideValueFieldName).EmitInstanceGetter<Card, bool?>());
		private readonly Lazy<Action<Card, bool?>> SetOverrideValue = new(() => AccessTools.DeclaredField(typeof(Card), cardOverrideValueFieldName).EmitInstanceSetter<Card, bool?>());
		private readonly Lazy<Func<Card, bool>> GetOverridePermanent = new(() => AccessTools.DeclaredField(typeof(Card), cardOverridePermanentFieldName).EmitInstanceGetter<Card, bool>());
		private readonly Lazy<Action<Card, bool>> SetOverridePermanent = new(() => AccessTools.DeclaredField(typeof(Card), cardOverridePermanentFieldName).EmitInstanceSetter<Card, bool>());

		public override void SetPermanentOverride(Card card, OverridesModData overrides, bool? overrideValue)
		{
			base.SetPermanentOverride(card, overrides, overrideValue);
			this.FixCardFields(card, overrides);
		}

		public override void SetTemporaryOverride(Card card, OverridesModData overrides, bool? overrideValue)
		{
			base.SetTemporaryOverride(card, overrides, overrideValue);
			this.FixCardFields(card, overrides);
		}

		public override void FixModData(Card card, OverridesModData overrides)
		{
			var fieldValue = this.GetOverrideValue.Value(card);
			var isTemporary = fieldValue is null ? overrides.Temporary.ContainsKey(this.UniqueName) : !this.GetOverridePermanent.Value(card);

			if (isTemporary)
				this.SetTemporaryOverride(card, overrides, fieldValue);
			else
				this.SetPermanentOverride(card, overrides, fieldValue);
		}

		private void FixCardFields(Card card, OverridesModData overrides)
		{
			this.SetOverrideValue.Value(card, overrides.Temporary.TryGetValue(this.UniqueName, out var temporaryOverride) ? temporaryOverride : (overrides.Permanent.TryGetValue(this.UniqueName, out var permanentOverride) ? permanentOverride : null));
			this.SetOverridePermanent.Value(card, !overrides.Temporary.ContainsKey(this.UniqueName) && overrides.Permanent.ContainsKey(this.UniqueName));
		}
	}

	private class TemporaryVanillaEntry(
		IModManifest modOwner
	) : VanillaEntry(modOwner, nameof(CardData.temporary))
	{
		private readonly Func<Card, bool?> GetOverrideValue = c => c.temporaryOverride;
		private readonly Action<Card, bool?> SetOverrideValue = (c, v) => c.temporaryOverride = v;

		public override void SetPermanentOverride(Card card, OverridesModData overrides, bool? overrideValue)
		{
			base.SetPermanentOverride(card, overrides, overrideValue);
			this.FixCardFields(card, overrides);
		}

		public override void SetTemporaryOverride(Card card, OverridesModData overrides, bool? overrideValue)
			=> this.SetPermanentOverride(card, overrides, overrideValue);

		public override void FixModData(Card card, OverridesModData overrides)
		{
			var fieldValue = this.GetOverrideValue(card);
			this.SetPermanentOverride(card, overrides, fieldValue);
		}

		private void FixCardFields(Card card, OverridesModData overrides)
			=> this.SetOverrideValue(card, overrides.Permanent.TryGetValue(this.UniqueName, out var temporaryOverride) ? temporaryOverride : null);
	}

	private class ModDataBasedPermanenceVanillaEntry(
		IModManifest modOwner,
		string dataFieldName,
		string? cardOverrideValueFieldName = null,
		bool isPermanentByDefault = false
	) : VanillaEntry(modOwner, dataFieldName)
	{
		private readonly Lazy<Func<Card, bool?>>? GetOverrideValue = cardOverrideValueFieldName is null ? null : new (() => AccessTools.DeclaredField(typeof(Card), cardOverrideValueFieldName).EmitInstanceGetter<Card, bool?>());
		private readonly Lazy<Action<Card, bool?>>? SetOverrideValue = cardOverrideValueFieldName is null ? null : new (() => AccessTools.DeclaredField(typeof(Card), cardOverrideValueFieldName).EmitInstanceSetter<Card, bool?>());

		public override void SetPermanentOverride(Card card, OverridesModData overrides, bool? overrideValue)
		{
			base.SetPermanentOverride(card, overrides, overrideValue);
			this.FixCardFields(card, overrides);
		}

		public override void SetTemporaryOverride(Card card, OverridesModData overrides, bool? overrideValue)
		{
			base.SetTemporaryOverride(card, overrides, overrideValue);
			this.FixCardFields(card, overrides);
		}

		public override void FixModData(Card card, OverridesModData overrides)
		{
			if (this.GetOverrideValue is null)
				return;

			var fieldValue = this.GetOverrideValue.Value(card);
			if (isPermanentByDefault)
				this.SetPermanentOverride(card, overrides, fieldValue);
			else
				this.SetTemporaryOverride(card, overrides, fieldValue);
		}

		private void FixCardFields(Card card, OverridesModData overrides)
			=> this.SetOverrideValue?.Value(card, overrides.Temporary.TryGetValue(this.UniqueName, out var temporaryOverride) ? temporaryOverride : (overrides.Permanent.TryGetValue(this.UniqueName, out var permanentOverride) ? permanentOverride : null));
	}

	private struct OverridesModData
	{
		public Dictionary<string, bool> Permanent = [];
		public Dictionary<string, bool> Temporary = [];

		public OverridesModData()
		{
		}
	}

	private readonly Dictionary<string, ModdedEntry> UniqueNameToEntry = [];
	private readonly Dictionary<Card, IReadOnlyDictionary<ICardTraitEntry, CardTraitState>> CardTraitStateCache = [];
	private readonly HashSet<Card> CurrentlyCreatingCardTraitStates = [];
	private readonly Lazy<IReadOnlyDictionary<string, ICardTraitEntry>> SynthesizedVanillaEntries;
	private readonly IModManifest ModManagerModManifest;
	private readonly ModDataManager ModDataManager;

	internal ManagedEvent<GetDynamicInnateCardTraitOverridesEventArgs> OnGetDynamicInnateCardTraitOverridesEvent { get; }
	internal ManagedEvent<GetFinalDynamicCardTraitOverridesEventArgs> OnGetFinalDynamicCardTraitOverridesEvent { get; }

	internal readonly Lazy<ICardTraitEntry> ExhaustCardTrait;
	internal readonly Lazy<ICardTraitEntry> RetainCardTrait;
	internal readonly Lazy<ICardTraitEntry> RecycleCardTrait;
	internal readonly Lazy<ICardTraitEntry> UnplayableCardTrait;
	internal readonly Lazy<ICardTraitEntry> TemporaryCardTrait;
	internal readonly Lazy<ICardTraitEntry> BuoyantCardTrait;
	internal readonly Lazy<ICardTraitEntry> SingleUseCardTrait;
	internal readonly Lazy<ICardTraitEntry> InfiniteCardTrait;

	public CardTraitManager(Func<IModManifest, ILogger> loggerProvider, IModManifest vanillaModManifest, IModManifest modManagerModManifest, ModDataManager modDataManager)
	{
		this.ModManagerModManifest = modManagerModManifest;
		this.ModDataManager = modDataManager;
		
		this.OnGetDynamicInnateCardTraitOverridesEvent = new((_, mod, exception) =>
		{
			var logger = loggerProvider(mod);
			logger.LogError("Mod failed in `{Event}`: {Exception}", nameof(this.OnGetFinalDynamicCardTraitOverridesEvent), exception);
		})
		{
			ModifyEventArgsBetweenSubscribers = (IModManifest? previousSubscriber, IModManifest? _, object? _, ref GetDynamicInnateCardTraitOverridesEventArgs args) =>
			{
				if (previousSubscriber is null)
					return;
				if (args.Overrides.Count == 0)
					return;

				var newDynamicInnateTraitOverrides = new Dictionary<ICardTraitEntry, bool>(args.DynamicInnateTraitOverrides);
				foreach (var (overrideTrait, nullableOverrideValue) in args.Overrides)
				{
					if (nullableOverrideValue is { } overrideValue)
						newDynamicInnateTraitOverrides[overrideTrait] = overrideValue;
					else
						newDynamicInnateTraitOverrides.Remove(overrideTrait);
				}

				args = args with
				{
					DynamicInnateTraitOverrides = newDynamicInnateTraitOverrides,
					Overrides = []
				};
			}
		};

		this.OnGetFinalDynamicCardTraitOverridesEvent = new((_, mod, exception) =>
		{
			var logger = loggerProvider(mod);
			logger.LogError("Mod failed in `{Event}`: {Exception}", nameof(this.OnGetFinalDynamicCardTraitOverridesEvent), exception);
		})
		{
			ModifyEventArgsBetweenSubscribers = (IModManifest? previousSubscriber, IModManifest? _, object? _, ref GetFinalDynamicCardTraitOverridesEventArgs args) =>
			{
				if (previousSubscriber is null)
					return;
				if (args.Overrides.Count == 0)
					return;

				var newTraitStates = new Dictionary<ICardTraitEntry, CardTraitState>(args.TraitStates);
				foreach (var (overrideTrait, overrideValue) in args.Overrides)
					newTraitStates[overrideTrait] = newTraitStates[overrideTrait] with { FinalDynamicOverride = overrideValue };

				args = args with
				{
					TraitStates = newTraitStates,
					Overrides = []
				};
			}
		};

		this.ExhaustCardTrait = new(() => new VariablePermanenceVanillaEntry(vanillaModManifest, nameof(CardData.exhaust), nameof(Card.exhaustOverride), nameof(Card.exhaustOverrideIsPermanent)));
		this.RetainCardTrait = new(() => new VariablePermanenceVanillaEntry(vanillaModManifest, nameof(CardData.retain), nameof(Card.retainOverride), nameof(Card.retainOverrideIsPermanent)));
		this.RecycleCardTrait = new(() => new VariablePermanenceVanillaEntry(vanillaModManifest, nameof(CardData.recycle), nameof(Card.recycleOverride), nameof(Card.recycleOverrideIsPermanent)));
		this.UnplayableCardTrait = new(() => new VariablePermanenceVanillaEntry(vanillaModManifest, nameof(CardData.unplayable), nameof(Card.unplayableOverride), nameof(Card.unplayableOverrideIsPermanent)));
		this.BuoyantCardTrait = new(() => new VariablePermanenceVanillaEntry(vanillaModManifest, nameof(CardData.buoyant), nameof(Card.buoyantOverride), nameof(Card.buoyantOverrideIsPermanent)));
		this.TemporaryCardTrait = new(() => new TemporaryVanillaEntry(vanillaModManifest));
		this.SingleUseCardTrait = new(() => new ModDataBasedPermanenceVanillaEntry(vanillaModManifest, nameof(CardData.singleUse), nameof(Card.singleUseOverride), isPermanentByDefault: true));
		this.InfiniteCardTrait = new(() => new ModDataBasedPermanenceVanillaEntry(vanillaModManifest, nameof(CardData.infinite)));

		this.SynthesizedVanillaEntries = new(() => new List<ICardTraitEntry>
		{
			this.ExhaustCardTrait.Value,
			this.RetainCardTrait.Value,
			this.RecycleCardTrait.Value,
			this.UnplayableCardTrait.Value,
			this.BuoyantCardTrait.Value,
			this.TemporaryCardTrait.Value,
			this.SingleUseCardTrait.Value,
			this.InfiniteCardTrait.Value,
		}.ToDictionary(t => t.UniqueName));

		CardPatches.OnGetTooltips += this.OnGetCardTooltips;
		CardPatches.OnRenderTraits += this.OnRenderTraits;
		CardPatches.OnGettingDataWithOverrides += this.OnGettingDataWithOverrides;
		CardPatches.OnMidGetDataWithOverrides += this.OnMidGetDataWithOverrides;
		CombatPatches.OnReturnCardsToDeck += this.OnReturnCardsToDeck;
		StatePatches.OnUpdate += this.OnStateUpdate;
	}

	private void OnRenderTraits(object? sender, CardPatches.TraitRenderEventArgs e)
	{
		foreach (var trait in this.GetActiveCardTraits(e.State, e.Card).Where(t => t is not VanillaEntry))
			if (trait.Configuration.Icon(e.State, e.Card) is { } icon)
				Draw.Sprite(icon, e.Position.x, e.Position.y - 8 * e.CardTraitIndex++);
	}

	private void OnGetCardTooltips(object? sender, CardPatches.TooltipsEventArgs e)
		=> e.TooltipsEnumerator = e.TooltipsEnumerator.Concat(
			this.GetActiveCardTraits(e.State, e.Card)
				.Where(t => t is not VanillaEntry)
				.SelectMany(t => t.Configuration.Tooltips?.Invoke(e.State, e.Card) ?? [])
		);

	private void OnGettingDataWithOverrides(object? sender, CardPatches.GettingDataWithOverridesEventArgs e)
	{
		if (e.State == DB.fakeState)
			return;
		if (MG.inst.g.metaRoute is not null && MG.inst.g.metaRoute is { subRoute: Codex })
			return;
		this.FixModData(e.Card);
	}

	private void OnMidGetDataWithOverrides(object? sender, CardPatches.MidGetDataWithOverridesEventArgs e)
	{
		e.CurrentData.exhaust = this.IsCardTraitActive(e.State, e.Card, this.ExhaustCardTrait.Value);
		e.CurrentData.retain = this.IsCardTraitActive(e.State, e.Card, this.RetainCardTrait.Value);
		e.CurrentData.recycle = this.IsCardTraitActive(e.State, e.Card, this.RecycleCardTrait.Value);
		e.CurrentData.unplayable = this.IsCardTraitActive(e.State, e.Card, this.UnplayableCardTrait.Value);
		e.CurrentData.temporary = this.IsCardTraitActive(e.State, e.Card, this.TemporaryCardTrait.Value);
		e.CurrentData.buoyant = this.IsCardTraitActive(e.State, e.Card, this.BuoyantCardTrait.Value);
		e.CurrentData.singleUse = this.IsCardTraitActive(e.State, e.Card, this.SingleUseCardTrait.Value);
		e.CurrentData.infinite = this.IsCardTraitActive(e.State, e.Card, this.InfiniteCardTrait.Value);
	}

	private void OnReturnCardsToDeck(object? sender, State state)
	{
		foreach (var card in state.deck)
		{
			if (!this.ModDataManager.TryGetModData<OverridesModData>(this.ModManagerModManifest, card, "CustomTraitOverrides", out var overrides))
				continue;

			foreach (var temporaryTraitName in overrides.Temporary.Keys.ToList())
			{
				if (this.LookupByUniqueName(temporaryTraitName) is not IReadWriteCardTraitEntry trait)
					continue;

				trait.SetTemporaryOverride(card, overrides, null);
			}
		}
	}

	private void OnStateUpdate(object? sender, State state)
		=> this.CardTraitStateCache.Clear();

	public ICardTraitEntry? LookupByUniqueName(string uniqueName)
	{
		if (this.SynthesizedVanillaEntries.Value.TryGetValue(uniqueName, out var vanillaEntry))
			return vanillaEntry;
		return this.UniqueNameToEntry.GetValueOrDefault(uniqueName);
	}

	public ICardTraitEntry RegisterTrait(IModManifest owner, string name, CardTraitConfiguration configuration)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		if (this.UniqueNameToEntry.ContainsKey(uniqueName))
			throw new ArgumentException($"A card trait with the unique name `{uniqueName}` is already registered", nameof(name));
		var entry = new ModdedEntry(owner, uniqueName, configuration);
		this.UniqueNameToEntry.Add(uniqueName, entry);
		return entry;
	}

	public IReadOnlySet<ICardTraitEntry> GetActiveCardTraits(State state, Card card)
		=> this.GetAllCardTraits(state, card)
			.Where(kvp => kvp.Value.IsActive)
			.Select(kvp => kvp.Key)
			.ToHashSet();

	public IReadOnlyDictionary<ICardTraitEntry, CardTraitState> GetAllCardTraits(State state, Card card)
		=> this.ObtainCardTraitStates(state, card);

	public CardTraitState GetCardTraitState(State state, Card card, ICardTraitEntry trait)
		=> this.ObtainCardTraitStates(state, card).TryGetValue(trait, out var traitState) ? traitState : new();

	public bool IsCardTraitActive(State state, Card card, ICardTraitEntry trait)
		=> this.GetCardTraitState(state, card, trait).IsActive;

	public void SetCardTraitOverride(Card card, ICardTraitEntry trait, bool? overrideValue, bool permanent)
	{
		if (trait is not IReadWriteCardTraitEntry rwTrait)
			throw new NotImplementedException($"Internal error: trait {trait.UniqueName} is supposed to implement the private interface {nameof(IReadWriteCardTraitEntry)}");

		var overrides = this.ModDataManager.ObtainModData<OverridesModData>(this.ModManagerModManifest, card, "CustomTraitOverrides");
		if (permanent)
			rwTrait.SetPermanentOverride(card, overrides, overrideValue);
		else
			rwTrait.SetTemporaryOverride(card, overrides, overrideValue);
		this.CardTraitStateCache.Remove(card);
	}

	private void FixModData(Card card, OverridesModData? overrides = null)
	{
		var nonNullOverrides = overrides ?? this.ModDataManager.ObtainModData<OverridesModData>(this.ModManagerModManifest, card, "CustomTraitOverrides");
		foreach (var trait in this.SynthesizedVanillaEntries.Value.Values)
			this.FixModData(card, trait, nonNullOverrides);
	}

	private void FixModData(Card card, ICardTraitEntry trait, OverridesModData? overrides = null)
	{
		if (trait is not IReadWriteCardTraitEntry rwTrait)
			throw new NotImplementedException($"Internal error: trait {trait.UniqueName} is supposed to implement the private interface {nameof(IReadWriteCardTraitEntry)}");

		var nonNullOverrides = overrides ?? this.ModDataManager.ObtainModData<OverridesModData>(this.ModManagerModManifest, card, "CustomTraitOverrides");
		rwTrait.FixModData(card, nonNullOverrides);
	}

	private IReadOnlyDictionary<ICardTraitEntry, CardTraitState> ObtainCardTraitStates(State state, Card card)
	{
		if (!this.CardTraitStateCache.TryGetValue(card, out var states))
		{
			states = this.CreateCardTraitStates(state, card);
			this.CardTraitStateCache[card] = states;
		}
		return states;
	}

	private IReadOnlyDictionary<ICardTraitEntry, CardTraitState> CreateCardTraitStates(State state, Card card)
	{
		Dictionary<ICardTraitEntry, CardTraitState> results = [];
		HashSet<ICardTraitEntry> innateTraits = [];
		var overrides = this.ModDataManager.TryGetModData<OverridesModData>(this.ModManagerModManifest, card, "CustomTraitOverrides", out var modDataOverrides) ? modDataOverrides : new();
		this.FixModData(card, overrides);

		var wasCurrentlyCreatingCardTraitStates = this.CurrentlyCreatingCardTraitStates.Contains(card);
		this.CurrentlyCreatingCardTraitStates.Add(card);

		var data = wasCurrentlyCreatingCardTraitStates ? new() : card.GetData(state);
		var innateCustomTraits = wasCurrentlyCreatingCardTraitStates ? [] : ((card as IHasCustomCardTraits)?.GetInnateTraits(state).ToHashSet() ?? []);
		
		foreach (var trait in this.SynthesizedVanillaEntries.Value.Values)
			HandleTrait(trait);
		foreach (var trait in this.UniqueNameToEntry.Values)
			HandleTrait(trait);

		if (wasCurrentlyCreatingCardTraitStates)
			return results;

		var dynamicInnateOverridesEventArgs = this.OnGetDynamicInnateCardTraitOverridesEvent.Raise(null, new()
		{
			State = state,
			Card = card,
			CardData = data,
			InnateTraits = innateTraits,
			DynamicInnateTraitOverrides = ImmutableDictionary<ICardTraitEntry, bool>.Empty,
			Overrides = [],
		});

		foreach (var (trait, overrideValue) in dynamicInnateOverridesEventArgs.DynamicInnateTraitOverrides)
			results[trait] = results[trait] with { DynamicInnateOverride = overrideValue };
		
		var finalDynamicOverridesEventArgs = this.OnGetFinalDynamicCardTraitOverridesEvent.Raise(null, new()
		{
			State = state,
			Card = card,
			CardData = data,
			TraitStates = results,
			Overrides = []
		});

		if (!wasCurrentlyCreatingCardTraitStates)
			this.CurrentlyCreatingCardTraitStates.Remove(card);

		return finalDynamicOverridesEventArgs.TraitStates;

		void HandleTrait(ICardTraitEntry trait)
		{
			if (trait is not IReadWriteCardTraitEntry rwTrait)
				throw new NotImplementedException($"Internal error: trait {trait.UniqueName} is supposed to implement the private interface {nameof(IReadWriteCardTraitEntry)}");

			var isInnatelyActive = rwTrait.IsInnatelyActive(card, data, innateCustomTraits);
			if (isInnatelyActive)
				innateTraits.Add(trait);
			
			results[trait] = new()
			{
				Innate = isInnatelyActive,
				PermanentOverride = rwTrait.GetPermanentOverride(card, overrides),
				TemporaryOverride = rwTrait.GetTemporaryOverride(card, overrides),
			};
		}
	}
}

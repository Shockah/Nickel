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

		bool UpdateOverridesModDataFromFieldsIfNeeded(Card card, OverridesModData overrides);
		bool UpdateFieldsFromCardTraitStateIfNeeded(Card card, OverridesModData overrides, CardTraitState cardTraitState, bool realOverrides);
	}

	private sealed class ModdedEntry(IModManifest modOwner, string uniqueName, CardTraitConfiguration configuration)
		: IReadWriteCardTraitEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = uniqueName;
		public CardTraitConfiguration Configuration { get; } = configuration;

		public override int GetHashCode()
			=> this.UniqueName.GetHashCode();

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

		public bool UpdateOverridesModDataFromFieldsIfNeeded(Card card, OverridesModData overrides)
			=> false;

		public bool UpdateFieldsFromCardTraitStateIfNeeded(Card card, OverridesModData overrides, CardTraitState cardTraitState, bool realOverrides)
			=> false;
	}

	private abstract class VanillaEntry(IModManifest modOwner, string dataFieldName)
		: IReadWriteCardTraitEntry
	{
		public IModManifest ModOwner { get; } = modOwner;
		public string UniqueName { get; } = dataFieldName;
		public CardTraitConfiguration Configuration { get; } = new()
		{
			Icon = (_, _) => Enum.TryParse<Spr>($"icons_{dataFieldName}", out var icon) ? icon : null,
			Name = _ => Loc.T($"cardtrait.{dataFieldName}.name"),
			Tooltips = (_, _) => [new TTGlossary($"cardtrait.{dataFieldName}")]
		};

		private readonly Lazy<Func<CardData, bool>> GetDataValue = new(() => AccessTools.DeclaredField(typeof(CardData), dataFieldName).EmitInstanceGetter<CardData, bool>());

		public override int GetHashCode()
			=> this.UniqueName.GetHashCode();

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

		public abstract bool UpdateOverridesModDataFromFieldsIfNeeded(Card card, OverridesModData overrides);

		public abstract bool UpdateFieldsFromCardTraitStateIfNeeded(Card card, OverridesModData overrides, CardTraitState cardTraitState, bool realOverrides);
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

		public override int GetHashCode()
			=> this.UniqueName.GetHashCode();

		public override void SetPermanentOverride(Card card, OverridesModData overrides, bool? overrideValue)
		{
			base.SetPermanentOverride(card, overrides, overrideValue);

			if (overrideValue is null)
				overrides.Permanent.Remove(this.UniqueName);
			else
				overrides.Permanent[this.UniqueName] = overrideValue.Value;
		}

		public override void SetTemporaryOverride(Card card, OverridesModData overrides, bool? overrideValue)
		{
			base.SetTemporaryOverride(card, overrides, overrideValue);
			
			if (overrideValue is null)
				overrides.Temporary.Remove(this.UniqueName);
			else
				overrides.Temporary[this.UniqueName] = overrideValue.Value;
		}

		public override bool UpdateOverridesModDataFromFieldsIfNeeded(Card card, OverridesModData overrides)
		{
			var overrideValue = this.GetOverrideValue.Value(card);
			var overridePermanent = this.GetOverridePermanent.Value(card);

			if (overrides.LastOverrideValue.GetValueOrDefault(cardOverrideValueFieldName) == overrideValue && overrides.LastOverridePermanent.GetValueOrDefault(cardOverridePermanentFieldName) == overridePermanent)
				return false;

			if (overridePermanent)
			{
				overrides.Temporary.Remove(this.UniqueName);

				if (overrideValue is null)
					overrides.Permanent.Remove(this.UniqueName);
				else
					overrides.Permanent[this.UniqueName] = overrideValue.Value;
			}
			else
			{
				if (overrideValue is null)
					overrides.Temporary.Remove(this.UniqueName);
				else
					overrides.Temporary[this.UniqueName] = overrideValue.Value;
			}

			overrides.LastOverrideValue[cardOverrideValueFieldName] = overrideValue;
			overrides.LastOverridePermanent[cardOverridePermanentFieldName] = overridePermanent;
			return true;
		}

		public override bool UpdateFieldsFromCardTraitStateIfNeeded(Card card, OverridesModData overrides, CardTraitState cardTraitState, bool realOverrides)
		{
			var overrideValue = realOverrides ? cardTraitState.TemporaryOverride ?? cardTraitState.PermanentOverride : cardTraitState.CurrentOverride;
			var overridePermanent = cardTraitState.TemporaryOverride is null && cardTraitState.PermanentOverride is not null;

			if (overrides.LastOverrideValue.GetValueOrDefault(cardOverrideValueFieldName) == overrideValue && overrides.LastOverridePermanent.GetValueOrDefault(cardOverridePermanentFieldName) == overridePermanent)
				return false;
			
			this.SetOverrideValue.Value(card, overrideValue);
			this.SetOverridePermanent.Value(card, overridePermanent);
			overrides.LastOverrideValue[cardOverrideValueFieldName] = overrideValue;
			overrides.LastOverrideValue[cardOverridePermanentFieldName] = overridePermanent;
			return true;
		}
	}

	private class TemporaryVanillaEntry(
		IModManifest modOwner
	) : VanillaEntry(modOwner, nameof(CardData.temporary))
	{
		private readonly Func<Card, bool?> GetOverrideValue = c => c.temporaryOverride;
		private readonly Action<Card, bool?> SetOverrideValue = (c, v) => c.temporaryOverride = v;

		public override int GetHashCode()
			=> this.UniqueName.GetHashCode();

		public override void SetPermanentOverride(Card card, OverridesModData overrides, bool? overrideValue)
		{
			base.SetPermanentOverride(card, overrides, overrideValue);
			
			if (overrideValue is null)
				overrides.Permanent.Remove(this.UniqueName);
			else
				overrides.Permanent[this.UniqueName] = overrideValue.Value;
		}

		public override void SetTemporaryOverride(Card card, OverridesModData overrides, bool? overrideValue)
			=> this.SetPermanentOverride(card, overrides, overrideValue);

		public override bool UpdateOverridesModDataFromFieldsIfNeeded(Card card, OverridesModData overrides)
		{
			var overrideValue = this.GetOverrideValue(card);

			if (overrides.LastOverrideValue.GetValueOrDefault(nameof(Card.temporaryOverride)) == overrideValue)
				return false;
			
			if (overrideValue is null)
				overrides.Permanent.Remove(this.UniqueName);
			else
				overrides.Permanent[this.UniqueName] = overrideValue.Value;

			overrides.LastOverrideValue[nameof(Card.temporaryOverride)] = overrideValue;
			return true;
		}

		public override bool UpdateFieldsFromCardTraitStateIfNeeded(Card card, OverridesModData overrides, CardTraitState cardTraitState, bool realOverrides)
		{
			var overrideValue = realOverrides ? cardTraitState.TemporaryOverride ?? cardTraitState.PermanentOverride : cardTraitState.CurrentOverride;

			if (overrides.LastOverrideValue.GetValueOrDefault(nameof(Card.temporaryOverride)) == overrideValue)
				return false;
			
			this.SetOverrideValue(card, overrideValue);
			overrides.LastOverrideValue[nameof(Card.temporaryOverride)] = overrideValue;
			return true;
		}
	}

	private class ModDataBasedPermanenceVanillaEntry(
		IModManifest modOwner,
		string dataFieldName,
		string? cardOverrideValueFieldName = null,
		bool isPermanentByDefault = true
	) : VanillaEntry(modOwner, dataFieldName)
	{
		private readonly Lazy<Func<Card, bool?>>? GetOverrideValue = cardOverrideValueFieldName is null ? null : new (() => AccessTools.DeclaredField(typeof(Card), cardOverrideValueFieldName).EmitInstanceGetter<Card, bool?>());
		private readonly Lazy<Action<Card, bool?>>? SetOverrideValue = cardOverrideValueFieldName is null ? null : new (() => AccessTools.DeclaredField(typeof(Card), cardOverrideValueFieldName).EmitInstanceSetter<Card, bool?>());

		public override int GetHashCode()
			=> this.UniqueName.GetHashCode();

		public override void SetPermanentOverride(Card card, OverridesModData overrides, bool? overrideValue)
		{
			base.SetPermanentOverride(card, overrides, overrideValue);
			
			if (overrideValue is null)
				overrides.Permanent.Remove(this.UniqueName);
			else
				overrides.Permanent[this.UniqueName] = overrideValue.Value;
		}

		public override void SetTemporaryOverride(Card card, OverridesModData overrides, bool? overrideValue)
		{
			base.SetTemporaryOverride(card, overrides, overrideValue);
			
			if (overrideValue is null)
				overrides.Temporary.Remove(this.UniqueName);
			else
				overrides.Temporary[this.UniqueName] = overrideValue.Value;
		}

		public override bool UpdateOverridesModDataFromFieldsIfNeeded(Card card, OverridesModData overrides)
		{
			if (this.GetOverrideValue is null || cardOverrideValueFieldName is null)
				return false;
			
			var overrideValue = this.GetOverrideValue.Value(card);

			if (overrides.LastOverrideValue.GetValueOrDefault(cardOverrideValueFieldName) == overrideValue)
				return false;

			if (isPermanentByDefault)
			{
				overrides.Temporary.Remove(this.UniqueName);

				if (overrideValue is null)
					overrides.Permanent.Remove(this.UniqueName);
				else
					overrides.Permanent[this.UniqueName] = overrideValue.Value;
			}
			else
			{
				if (overrideValue is null)
					overrides.Temporary.Remove(this.UniqueName);
				else
					overrides.Temporary[this.UniqueName] = overrideValue.Value;
			}

			overrides.LastOverrideValue[cardOverrideValueFieldName] = overrideValue;
			return true;
		}

		public override bool UpdateFieldsFromCardTraitStateIfNeeded(Card card, OverridesModData overrides, CardTraitState cardTraitState, bool realOverrides)
		{
			if (this.SetOverrideValue is null || cardOverrideValueFieldName is null)
				return false;
			
			var overrideValue = realOverrides ? cardTraitState.TemporaryOverride ?? cardTraitState.PermanentOverride : cardTraitState.CurrentOverride;

			if (overrides.LastOverrideValue.GetValueOrDefault(cardOverrideValueFieldName) == overrideValue)
				return false;
			
			this.SetOverrideValue.Value(card, overrideValue);
			overrides.LastOverrideValue[nameof(Card.temporaryOverride)] = overrideValue;
			return true;
		}
	}

	private sealed class OverridesModData
	{
		public readonly Dictionary<string, bool> Permanent = [];
		public readonly Dictionary<string, bool> Temporary = [];
		public readonly Dictionary<string, bool?> LastOverrideValue = [];
		public readonly Dictionary<string, bool> LastOverridePermanent = [];
	}

	private readonly Dictionary<string, ModdedEntry> UniqueNameToEntry = [];
	private readonly Dictionary<Card, IReadOnlyDictionary<ICardTraitEntry, CardTraitState>> CardTraitStateCache = [];
	private readonly HashSet<Card> CurrentlyCreatingCardTraitStates = [];
	private readonly Dictionary<string, ICardTraitEntry> SynthesizedVanillaEntries;
	private readonly IModManifest ModLoaderModManifest;
	private readonly ModDataManager ModDataManager;

	internal readonly ManagedEvent<GetDynamicInnateCardTraitOverridesEventArgs> OnGetDynamicInnateCardTraitOverridesEvent;
	internal readonly ManagedEvent<GetFinalDynamicCardTraitOverridesEventArgs> OnGetFinalDynamicCardTraitOverridesEvent;
	internal readonly ManagedEvent<SetCardTraitOverrideEventArgs> OnSetCardTraitOverrideEvent;

	internal readonly ICardTraitEntry ExhaustCardTrait;
	internal readonly ICardTraitEntry RetainCardTrait;
	internal readonly ICardTraitEntry RecycleCardTrait;
	internal readonly ICardTraitEntry UnplayableCardTrait;
	internal readonly ICardTraitEntry TemporaryCardTrait;
	internal readonly ICardTraitEntry BuoyantCardTrait;
	internal readonly ICardTraitEntry SingleUseCardTrait;
	internal readonly ICardTraitEntry InfiniteCardTrait;

	public CardTraitManager(Func<IModManifest, ILogger> loggerProvider, IModManifest vanillaModManifest, IModManifest modLoaderModManifest, ModDataManager modDataManager)
	{
		this.ModLoaderModManifest = modLoaderModManifest;
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

		this.OnSetCardTraitOverrideEvent = new((_, mod, exception) =>
		{
			var logger = loggerProvider(mod);
			logger.LogError("Mod failed in `{Event}`: {Exception}", nameof(this.OnSetCardTraitOverrideEvent), exception);
		});

		this.ExhaustCardTrait = new VariablePermanenceVanillaEntry(vanillaModManifest, nameof(CardData.exhaust), nameof(Card.exhaustOverride), nameof(Card.exhaustOverrideIsPermanent));
		this.RetainCardTrait = new VariablePermanenceVanillaEntry(vanillaModManifest, nameof(CardData.retain), nameof(Card.retainOverride), nameof(Card.retainOverrideIsPermanent));
		this.RecycleCardTrait = new VariablePermanenceVanillaEntry(vanillaModManifest, nameof(CardData.recycle), nameof(Card.recycleOverride), nameof(Card.recycleOverrideIsPermanent));
		this.UnplayableCardTrait = new VariablePermanenceVanillaEntry(vanillaModManifest, nameof(CardData.unplayable), nameof(Card.unplayableOverride), nameof(Card.unplayableOverrideIsPermanent));
		this.BuoyantCardTrait = new VariablePermanenceVanillaEntry(vanillaModManifest, nameof(CardData.buoyant), nameof(Card.buoyantOverride), nameof(Card.buoyantOverrideIsPermanent));
		this.TemporaryCardTrait = new TemporaryVanillaEntry(vanillaModManifest);
		this.SingleUseCardTrait = new ModDataBasedPermanenceVanillaEntry(vanillaModManifest, nameof(CardData.singleUse), nameof(Card.singleUseOverride));
		this.InfiniteCardTrait = new ModDataBasedPermanenceVanillaEntry(vanillaModManifest, nameof(CardData.infinite));

		this.SynthesizedVanillaEntries = new List<ICardTraitEntry>
		{
			this.ExhaustCardTrait,
			this.RetainCardTrait,
			this.RecycleCardTrait,
			this.UnplayableCardTrait,
			this.BuoyantCardTrait,
			this.TemporaryCardTrait,
			this.SingleUseCardTrait,
			this.InfiniteCardTrait,
		}.ToDictionary(t => t.UniqueName);

		CardPatches.OnGetTooltips += this.OnGetCardTooltips;
		CardPatches.OnRenderTraits += this.OnRenderTraits;
		CardPatches.OnGettingDataWithOverrides += this.OnGettingDataWithOverrides;
		CardPatches.OnMidGetDataWithOverrides += this.OnMidGetDataWithOverrides;
		CardPatches.OnCopyingWithNewId += this.OnCopyingWithNewId;
		CombatPatches.OnReturnCardsToDeck += this.OnReturnCardsToDeck;
		StatePatches.OnUpdating += this.OnStateUpdating;
		StatePatches.OnUpdate += this.OnStateUpdate;
	}

	private void OnRenderTraits(object? sender, ref CardPatches.TraitRenderEventArgs e)
	{
		foreach (var trait in this.GetActiveCardTraits(e.State, e.Card).Where(t => t is not VanillaEntry))
			if (trait.Configuration.Icon(e.State, e.Card) is { } icon)
				Draw.Sprite(icon, e.Position.x, e.Position.y - 8 * e.CardTraitIndex++);
	}

	private void OnGetCardTooltips(object? sender, ref CardPatches.TooltipsEventArgs e)
	{
		var state = e.State;
		var card = e.Card;
		
		e.TooltipsEnumerator = e.TooltipsEnumerator.Concat(
			this.GetActiveCardTraits(e.State, e.Card)
				.Where(t => t is not VanillaEntry)
				.SelectMany(t => t.Configuration.Tooltips?.Invoke(state, card) ?? [])
		);
	}

	private void OnGettingDataWithOverrides(object? sender, CardPatches.GettingDataWithOverridesEventArgs e)
	{
		if (e.State == DB.fakeState)
			return;
		if (MG.inst.g.metaRoute is not null && MG.inst.g.metaRoute is { subRoute: Codex })
			return;
		this.UpdateModDataFromFieldsIfNeeded(e.Card);
	}

	private void OnMidGetDataWithOverrides(object? sender, ref CardPatches.MidGetDataWithOverridesEventArgs e)
	{
		var traitState = (Dictionary<ICardTraitEntry, CardTraitState>)this.GetAllCardTraits(e.State, e.Card);
		e.CurrentData.exhaust = traitState[this.ExhaustCardTrait].IsActive;
		e.CurrentData.retain = traitState[this.RetainCardTrait].IsActive;
		e.CurrentData.recycle = traitState[this.RecycleCardTrait].IsActive;
		e.CurrentData.unplayable = traitState[this.UnplayableCardTrait].IsActive;
		e.CurrentData.temporary = traitState[this.TemporaryCardTrait].IsActive;
		e.CurrentData.buoyant = traitState[this.BuoyantCardTrait].IsActive;
		e.CurrentData.singleUse = traitState[this.SingleUseCardTrait].IsActive;
		e.CurrentData.infinite = traitState[this.InfiniteCardTrait].IsActive;
	}

	private void OnCopyingWithNewId(object? sender, Card original)
		=> this.UpdateModDataFromFieldsIfNeeded(original);

	private void OnReturnCardsToDeck(object? sender, State state)
	{
		foreach (var card in state.deck)
		{
			if (!this.ModDataManager.TryGetModData<OverridesModData>(this.ModLoaderModManifest, card, "CustomTraitOverrides", out var overrides))
				continue;

			foreach (var temporaryTraitName in overrides.Temporary.Keys.ToList())
			{
				if (this.LookupByUniqueName(temporaryTraitName) is not IReadWriteCardTraitEntry trait)
					continue;

				trait.SetTemporaryOverride(card, overrides, null);
			}
		}
	}

	private void OnStateUpdating(object? sender, State state)
		=> this.CardTraitStateCache.Clear();

	private void OnStateUpdate(object? sender, State state)
	{
		foreach (var (card, cardTraitStates) in this.CardTraitStateCache)
		{
			var overrides = this.ModDataManager.ObtainModData<OverridesModData>(this.ModLoaderModManifest, card, "CustomTraitOverrides");
			foreach (var (trait, cardTraitState) in cardTraitStates)
			{
				if (trait is not IReadWriteCardTraitEntry rwTrait)
					throw new NotImplementedException($"Internal error: trait {trait.UniqueName} is supposed to implement the private interface {nameof(IReadWriteCardTraitEntry)}");
				rwTrait.UpdateOverridesModDataFromFieldsIfNeeded(card, overrides);
				rwTrait.UpdateFieldsFromCardTraitStateIfNeeded(card, overrides, cardTraitState, realOverrides: false);
			}
		}
	}

	public ICardTraitEntry? LookupByUniqueName(string uniqueName)
	{
		if (this.SynthesizedVanillaEntries.TryGetValue(uniqueName, out var vanillaEntry))
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

	public void SetCardTraitOverride(State state, Card card, ICardTraitEntry trait, bool? overrideValue, bool permanent)
	{
		if (trait is not IReadWriteCardTraitEntry rwTrait)
			throw new NotImplementedException($"Internal error: trait {trait.UniqueName} is supposed to implement the private interface {nameof(IReadWriteCardTraitEntry)}");

		var overrides = this.ModDataManager.ObtainModData<OverridesModData>(this.ModLoaderModManifest, card, "CustomTraitOverrides");
		if (permanent)
			rwTrait.SetPermanentOverride(card, overrides, overrideValue);
		else
			rwTrait.SetTemporaryOverride(card, overrides, overrideValue);
		this.CardTraitStateCache.Remove(card);
		
		this.OnSetCardTraitOverrideEvent.Raise(null, new()
		{
			State = state,
			Card = card,
			CardTrait = trait,
			OverrideValue = overrideValue,
			IsPermanent = permanent,
		});
	}

	private void UpdateModDataFromFieldsIfNeeded(Card card, OverridesModData? overrides = null)
	{
		var nonNullOverrides = overrides ?? this.ModDataManager.ObtainModData<OverridesModData>(this.ModLoaderModManifest, card, "CustomTraitOverrides");
		foreach (var trait in this.SynthesizedVanillaEntries.Values)
			this.UpdateModDataFromFieldsIfNeeded(card, trait, nonNullOverrides);
	}

	private void UpdateModDataFromFieldsIfNeeded(Card card, ICardTraitEntry trait, OverridesModData? overrides = null)
	{
		if (trait is not IReadWriteCardTraitEntry rwTrait)
			throw new NotImplementedException($"Internal error: trait {trait.UniqueName} is supposed to implement the private interface {nameof(IReadWriteCardTraitEntry)}");

		var nonNullOverrides = overrides ?? this.ModDataManager.ObtainModData<OverridesModData>(this.ModLoaderModManifest, card, "CustomTraitOverrides");
		rwTrait.UpdateOverridesModDataFromFieldsIfNeeded(card, nonNullOverrides);
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
		var overrides = this.ModDataManager.ObtainModData<OverridesModData>(this.ModLoaderModManifest, card, "CustomTraitOverrides");
		this.UpdateModDataFromFieldsIfNeeded(card, overrides);

		var wasCurrentlyCreatingCardTraitStates = this.CurrentlyCreatingCardTraitStates.Contains(card);
		this.CurrentlyCreatingCardTraitStates.Add(card);

		var data = wasCurrentlyCreatingCardTraitStates ? new() : card.GetData(state);
		var innateCustomTraits = wasCurrentlyCreatingCardTraitStates ? [] : ((card as IHasCustomCardTraits)?.GetInnateTraits(state).ToHashSet() ?? []);
		
		foreach (var trait in this.SynthesizedVanillaEntries.Values)
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

		foreach (var (trait, traitState) in finalDynamicOverridesEventArgs.TraitStates)
			((IReadWriteCardTraitEntry)trait).UpdateFieldsFromCardTraitStateIfNeeded(card, overrides, traitState, realOverrides: false);
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

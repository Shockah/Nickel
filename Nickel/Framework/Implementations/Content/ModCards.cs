using System;
using System.Collections.Generic;

namespace Nickel;

internal sealed class ModCards(
	IModManifest modManifest,
	Func<CardManager> cardManagerProvider,
	Func<CardTraitManager> cardTraitManagerProvider
) : IModCards
{
	public IReadOnlyDictionary<string, ICardEntry> RegisteredCards
		=> this.RegisteredCardStorage;
	
	public IReadOnlyDictionary<string, ICardTraitEntry> RegisteredTraits
		=> this.RegisteredTraitStorage;
	
	private readonly Dictionary<string, ICardEntry> RegisteredCardStorage = [];
	private readonly Dictionary<string, ICardTraitEntry> RegisteredTraitStorage = [];
	private readonly Dictionary<EventHandler<GetVolatileCardTraitOverridesEventArgs>, EventHandler<GetFinalDynamicCardTraitOverridesEventArgs>> ObsoleteVolatileToFinalEventHandlers = [];

	public ICardEntry? LookupByCardType(Type cardType)
		=> cardManagerProvider().LookupByCardType(cardType);

	public ICardEntry? LookupByUniqueName(string uniqueName)
		=> cardManagerProvider().LookupByUniqueName(uniqueName);

	public ICardEntry RegisterCard(CardConfiguration configuration)
	{
		var entry = cardManagerProvider().RegisterCard(modManifest, configuration.CardType.Name, configuration);
		this.RegisteredCardStorage[configuration.CardType.Name] = entry;
		return entry;
	}

	public ICardEntry RegisterCard(string name, CardConfiguration configuration)
	{
		var entry = cardManagerProvider().RegisterCard(modManifest, name, configuration);
		this.RegisteredCardStorage[name] = entry;
		return entry;
	}

	public ICardTraitEntry ExhaustCardTrait
		=> cardTraitManagerProvider().ExhaustCardTrait;

	public ICardTraitEntry RetainCardTrait
		=> cardTraitManagerProvider().RetainCardTrait;

	public ICardTraitEntry RecycleCardTrait
		=> cardTraitManagerProvider().RecycleCardTrait;

	public ICardTraitEntry InfiniteCardTrait
		=> cardTraitManagerProvider().InfiniteCardTrait;

	public ICardTraitEntry UnplayableCardTrait
		=> cardTraitManagerProvider().UnplayableCardTrait;

	public ICardTraitEntry TemporaryCardTrait
		=> cardTraitManagerProvider().TemporaryCardTrait;

	public ICardTraitEntry BuoyantCardTrait
		=> cardTraitManagerProvider().BuoyantCardTrait;

	public ICardTraitEntry SingleUseCardTrait
		=> cardTraitManagerProvider().SingleUseCardTrait;

	public ICardTraitEntry? LookupTraitByUniqueName(string uniqueName)
		=> cardTraitManagerProvider().LookupByUniqueName(uniqueName);

	public ICardTraitEntry RegisterTrait(string name, CardTraitConfiguration configuration)
	{
		var entry = cardTraitManagerProvider().RegisterTrait(modManifest, name, configuration);
		this.RegisteredTraitStorage[name] = entry;
		return entry;
	}

	public IReadOnlySet<ICardTraitEntry> GetActiveCardTraits(State state, Card card)
		=> cardTraitManagerProvider().GetActiveCardTraits(state, card);

	public IReadOnlyDictionary<ICardTraitEntry, CardTraitState> GetAllCardTraits(State state, Card card)
		=> cardTraitManagerProvider().GetAllCardTraits(state, card);

	public bool IsCardTraitActive(State state, Card card, ICardTraitEntry trait)
		=> cardTraitManagerProvider().IsCardTraitActive(state, card, trait);

	public CardTraitState GetCardTraitState(State state, Card card, ICardTraitEntry trait)
		=> cardTraitManagerProvider().GetCardTraitState(state, card, trait);

	public void SetCardTraitOverride(State state, Card card, ICardTraitEntry trait, bool? overrideValue, bool permanent)
		=> cardTraitManagerProvider().SetCardTraitOverride(state, card, trait, overrideValue, permanent);

	public event EventHandler<GetVolatileCardTraitOverridesEventArgs> OnGetVolatileCardTraitOverrides
	{
		add
		{
			EventHandler<GetFinalDynamicCardTraitOverridesEventArgs> mappedHandler = (sender, args) =>
			{
				var mappedArgs = new GetVolatileCardTraitOverridesEventArgs
				{
					State = args.State,
					Card = args.Card,
					TraitStates = args.TraitStates,
					VolatileOverrides = []
				};
				
				value(sender, mappedArgs);
				
				foreach (var (overrideTrait, overrideValue) in mappedArgs.VolatileOverrides)
					args.SetOverride(overrideTrait, overrideValue);
			};
			this.ObsoleteVolatileToFinalEventHandlers[value] = mappedHandler;
			this.OnGetFinalDynamicCardTraitOverrides += mappedHandler;
		}
		remove
		{
			if (!this.ObsoleteVolatileToFinalEventHandlers.TryGetValue(value, out var mappedHandler))
				return;
			this.OnGetFinalDynamicCardTraitOverrides -= mappedHandler;
		}
	}

	public event EventHandler<GetDynamicInnateCardTraitOverridesEventArgs> OnGetDynamicInnateCardTraitOverrides
	{
		add => cardTraitManagerProvider().OnGetDynamicInnateCardTraitOverridesEvent.Add(value, modManifest);
		remove => cardTraitManagerProvider().OnGetDynamicInnateCardTraitOverridesEvent.Remove(value, modManifest);
	}

	public event EventHandler<GetFinalDynamicCardTraitOverridesEventArgs> OnGetFinalDynamicCardTraitOverrides
	{
		add => cardTraitManagerProvider().OnGetFinalDynamicCardTraitOverridesEvent.Add(value, modManifest);
		remove => cardTraitManagerProvider().OnGetFinalDynamicCardTraitOverridesEvent.Remove(value, modManifest);
	}

	public event EventHandler<SetCardTraitOverrideEventArgs> OnSetCardTraitOverride
	{
		add => cardTraitManagerProvider().OnSetCardTraitOverrideEvent.Add(value, modManifest);
		remove => cardTraitManagerProvider().OnSetCardTraitOverrideEvent.Remove(value, modManifest);
	}

	public bool IsCurrentlyCreatingCardTraitStates(State state, Card card)
		=> cardTraitManagerProvider().IsCurrentlyCreatingCardTraitStates(card);
}

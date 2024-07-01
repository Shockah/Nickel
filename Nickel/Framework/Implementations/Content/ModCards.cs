using Nickel.Models.Content;
using System;
using System.Collections.Generic;

namespace Nickel;

internal sealed class ModCards : IModCards
{
	private readonly IModManifest ModManifest;
	private readonly Func<CardManager> CardManagerProvider;
	private readonly Func<CardTraitManager> CardTraitManagerProvider;
	private readonly Dictionary<EventHandler<GetVolatileCardTraitOverridesEventArgs>, EventHandler<GetFinalDynamicCardTraitOverridesEventArgs>> ObsoleteVolatileToFinalEventHandlers = [];

	public ModCards(IModManifest modManifest, Func<CardManager> cardManagerProvider, Func<CardTraitManager> cardTraitManagerProvider)
	{
		this.ModManifest = modManifest;
		this.CardManagerProvider = cardManagerProvider;
		this.CardTraitManagerProvider = cardTraitManagerProvider;
	}

	public ICardEntry? LookupByCardType(Type cardType)
		=> this.CardManagerProvider().LookupByCardType(cardType);

	public ICardEntry? LookupByUniqueName(string uniqueName)
		=> this.CardManagerProvider().LookupByUniqueName(uniqueName);

	public ICardEntry RegisterCard(CardConfiguration configuration)
		=> this.CardManagerProvider().RegisterCard(this.ModManifest, configuration.CardType.Name, configuration);

	public ICardEntry RegisterCard(string name, CardConfiguration configuration)
		=> this.CardManagerProvider().RegisterCard(this.ModManifest, name, configuration);

	public ICardTraitEntry ExhaustCardTrait
		=> this.CardTraitManagerProvider().ExhaustCardTrait.Value;

	public ICardTraitEntry RetainCardTrait
		=> this.CardTraitManagerProvider().RetainCardTrait.Value;

	public ICardTraitEntry RecycleCardTrait
		=> this.CardTraitManagerProvider().RecycleCardTrait.Value;

	public ICardTraitEntry InfiniteCardTrait
		=> this.CardTraitManagerProvider().InfiniteCardTrait.Value;

	public ICardTraitEntry UnplayableCardTrait
		=> this.CardTraitManagerProvider().UnplayableCardTrait.Value;

	public ICardTraitEntry TemporaryCardTrait
		=> this.CardTraitManagerProvider().TemporaryCardTrait.Value;

	public ICardTraitEntry BuoyantCardTrait
		=> this.CardTraitManagerProvider().BuoyantCardTrait.Value;

	public ICardTraitEntry SingleUseCardTrait
		=> this.CardTraitManagerProvider().SingleUseCardTrait.Value;

	public ICardTraitEntry? LookupTraitByUniqueName(string uniqueName)
		=> this.CardTraitManagerProvider().LookupByUniqueName(uniqueName);

	public ICardTraitEntry RegisterTrait(string name, CardTraitConfiguration configuration)
		=> this.CardTraitManagerProvider().RegisterTrait(this.ModManifest, name, configuration);

	public IReadOnlySet<ICardTraitEntry> GetActiveCardTraits(State state, Card card)
		=> this.CardTraitManagerProvider().GetActiveCardTraits(state, card);

	public IReadOnlyDictionary<ICardTraitEntry, CardTraitState> GetAllCardTraits(State state, Card card)
		=> this.CardTraitManagerProvider().GetAllCardTraits(state, card);

	public bool IsCardTraitActive(State state, Card card, ICardTraitEntry trait)
		=> this.CardTraitManagerProvider().IsCardTraitActive(state, card, trait);

	public CardTraitState GetCardTraitState(State state, Card card, ICardTraitEntry trait)
		=> this.CardTraitManagerProvider().GetCardTraitState(state, card, trait);

	public void SetCardTraitOverride(State state, Card card, ICardTraitEntry trait, bool? overrideValue, bool permanent)
		=> this.CardTraitManagerProvider().SetCardTraitOverride(card, trait, overrideValue, permanent);

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
		add => this.CardTraitManagerProvider().OnGetDynamicInnateCardTraitOverridesEvent.Add(value, this.ModManifest);
		remove => this.CardTraitManagerProvider().OnGetDynamicInnateCardTraitOverridesEvent.Remove(value, this.ModManifest);
	}

	public event EventHandler<GetFinalDynamicCardTraitOverridesEventArgs> OnGetFinalDynamicCardTraitOverrides
	{
		add => this.CardTraitManagerProvider().OnGetFinalDynamicCardTraitOverridesEvent.Add(value, this.ModManifest);
		remove => this.CardTraitManagerProvider().OnGetFinalDynamicCardTraitOverridesEvent.Remove(value, this.ModManifest);
	}
}

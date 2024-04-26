using Nickel.Models.Content;
using System;
using System.Collections.Generic;

namespace Nickel;

public interface IModCards
{
	ICardEntry? LookupByCardType(Type cardType);
	ICardEntry? LookupByUniqueName(string uniqueName);
	ICardEntry RegisterCard(CardConfiguration configuration);
	ICardEntry RegisterCard(string name, CardConfiguration configuration);

	ICardTraitEntry ExhaustCardTrait { get; }
	ICardTraitEntry RetainCardTrait { get; }
	ICardTraitEntry RecycleCardTrait { get; }
	ICardTraitEntry InfiniteCardTrait { get; }
	ICardTraitEntry UnplayableCardTrait { get; }
	ICardTraitEntry TemporaryCardTrait { get; }
	ICardTraitEntry BuoyantCardTrait { get; }
	ICardTraitEntry SingleUseCardTrait { get; }

	ICardTraitEntry? LookupTraitByUniqueName(string uniqueName);
	ICardTraitEntry RegisterTrait(string name, CardTraitConfiguration configuration);
	IReadOnlySet<ICardTraitEntry> GetActiveCardTraits(State state, Card card);
	IReadOnlyDictionary<ICardTraitEntry, CardTraitState> GetAllCardTraits(State state, Card card);
	bool IsCardTraitActive(State state, Card card, ICardTraitEntry trait);
	CardTraitState GetCardTraitState(State state, Card card, ICardTraitEntry trait);
	void SetCardTraitOverride(State state, Card card, ICardTraitEntry trait, bool? overrideValue, bool permanent);

	event EventHandler<GetVolatileCardTraitOverridesEventArgs> OnGetVolatileCardTraitOverrides;
}

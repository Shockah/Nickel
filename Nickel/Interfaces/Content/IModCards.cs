using System;
using System.Collections.Generic;

namespace Nickel;

public interface IModCards
{
	ICardEntry? LookupByCardType(Type cardType);
	ICardEntry? LookupByUniqueName(string uniqueName);
	ICardEntry RegisterCard(CardConfiguration configuration);
	ICardEntry RegisterCard(string name, CardConfiguration configuration);

	ICardTraitEntry? LookupTraitByUniqueName(string uniqueName);
	ICardTraitEntry RegisterTrait(string name, CardTraitConfiguration configuration);
	public bool GetCardHasTrait(State state, Card card, ICardTraitEntry trait);
	public bool GetCardHasTrait(State state, Card card, string uniqueName);
	public IReadOnlySet<string> GetCardCurrentTraits(State state, Card card);
	public void AddCardTraitOverride(Card card, string uniqueName, bool overrideValue, bool isPermanent = false);
}

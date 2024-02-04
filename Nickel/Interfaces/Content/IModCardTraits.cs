namespace Nickel;

public interface IModCardTraits
{
	ICardTraitEntry? LookupByUniqueName(string uniqueName);
	ICardTraitEntry RegisterTrait(string name, CardTraitConfiguration configuration);
	public bool GetCardHasTrait(State state, Card card, ICardTraitEntry trait);
	public bool GetCardHasTrait(State state, Card card, string uniqueName);
	public void AddCardTraitOverride(Card card, string uniqueName, bool overrideValue, bool isPermanent = false);
}

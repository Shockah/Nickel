using System;

namespace Nickel.Essentials;

public sealed class ApiImplementation : IEssentialsApi
{
	public Type? GetExeCardTypeForDeck(Deck deck)
		=> ModEntry.Instance.GetExeCardTypeForDeck(deck);

	public Deck? GetDeckForExeCardType(Type type)
		=> ModEntry.Instance.GetDeckForExeCardType(type);

	public bool IsExeCardType(Type type)
		=> this.GetDeckForExeCardType(type) is not null;

	public UK ShipSelectionToggleUiKey
		=> ShipSelection.ShipSelectionToggleUiKey;
	
	public UK ShipSelectionUiKey
		=> ShipSelection.ShipSelectionUiKey;
}

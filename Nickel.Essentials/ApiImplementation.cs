using System;

namespace Nickel.Essentials;

public sealed class ApiImplementation : IEssentialsApi
{
	public void RegisterHook(IEssentialsApi.IHook hook, double priority = 0)
		=> ModEntry.Instance.Hooks.Register(hook, priority);

	public void UnregisterHook(IEssentialsApi.IHook hook)
		=> ModEntry.Instance.Hooks.Unregister(hook);

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

	public bool IsShowingShips
		=> ShipSelection.ShowingShips;

	public StarterShip? PreviewingShip
		=> ShipSelection.PreviewingShip;
}

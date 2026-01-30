namespace Nickel.Essentials;

public sealed class ApiImplementation : IEssentialsApi
{
	public void RegisterHook(IEssentialsApi.IHook hook, double priority = 0)
		=> ModEntry.Instance.Hooks.Register(hook, priority);

	public void UnregisterHook(IEssentialsApi.IHook hook)
		=> ModEntry.Instance.Hooks.Unregister(hook);

	public bool IsBlacklistedExeStarter(Deck deck)
		=> ModEntry.Instance.Settings.ProfileBased.Current.BlacklistedExeStarters.Contains(deck);

	public bool IsBlacklistedExeOffering(Deck deck)
		=> ModEntry.Instance.Settings.ProfileBased.Current.BlacklistedExeOfferings.Contains(deck);

	public UK ShipSelectionToggleUiKey
		=> ShipSelection.ShipSelectionToggleUiKey;
	
	public UK ShipSelectionUiKey
		=> ShipSelection.ShipSelectionUiKey;

	public bool IsShowingShips
		=> ShipSelection.ShowingShips;

	public StarterShip? PreviewingShip
		=> ShipSelection.PreviewingShip;
}

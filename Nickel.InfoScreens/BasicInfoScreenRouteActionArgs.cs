namespace Nickel.InfoScreens;

internal sealed class BasicInfoScreenRouteActionArgs : IInfoScreensApi.IBasicInfoScreenRoute.IAction.IArgs
{
	public G G { get; internal set; } = null!;
	public IInfoScreensApi.IBasicInfoScreenRoute Route { get; internal set; } = null!;
}

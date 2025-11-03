using System;

namespace Nickel.InfoScreens;

public sealed class ApiImplementation(IModManifest mod) : IInfoScreensApi
{
	public IInfoScreensApi.IBasicInfoScreenRoute CreateBasicInfoScreenRoute()
		=> new BasicInfoScreenRoute();

	public IInfoScreensApi.IBasicInfoScreenRoute.IParagraph CreateBasicInfoScreenParagraph(string text)
		=> new BasicInfoScreenRouteParagraph(text);

	public IInfoScreensApi.IBasicInfoScreenRoute.IAction CreateBasicInfoScreenAction(string title, Action<IInfoScreensApi.IBasicInfoScreenRoute.IAction.IArgs> action)
		=> new BasicInfoScreenRouteAction(title, action);

	public IInfoScreensApi.IInfoScreenEntry RequestInfoScreen(string name, Route route, double priority = 0)
		=> ModEntry.Instance.RequestInfoScreen(mod, name, route, priority);
}

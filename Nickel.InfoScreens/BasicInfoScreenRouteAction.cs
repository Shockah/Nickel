using System;

namespace Nickel.InfoScreens;

internal sealed class BasicInfoScreenRouteAction(
	string title,
	Action<IInfoScreensApi.IBasicInfoScreenRoute.IAction.IArgs> action
) : IInfoScreensApi.IBasicInfoScreenRoute.IAction
{
	public string Title { get; set; } = title;
	public Color? Color { get; set; }
	public Action<IInfoScreensApi.IBasicInfoScreenRoute.IAction.IArgs> Action { get; set; } = action;
	
	public IInfoScreensApi.IBasicInfoScreenRoute.IAction SetTitle(string value)
	{
		this.Title = value;
		return this;
	}

	public IInfoScreensApi.IBasicInfoScreenRoute.IAction SetColor(Color? value)
	{
		this.Color = value;
		return this;
	}
}

using System;

namespace Nickel.InfoScreens;

internal sealed class BasicInfoScreenRouteAction(
	string title,
	Action<IInfoScreensApi.IBasicInfoScreenRoute.IAction.IArgs> action
) : IInfoScreensApi.IBasicInfoScreenRoute.IAction
{
	public string Title { get; set; } = title;
	public Color? Color { get; set; }
	public bool RequiresConfirmation { get; set; }
	public Btn? ControllerKeybind { get; set; }
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

	public IInfoScreensApi.IBasicInfoScreenRoute.IAction SetRequiresConfirmation(bool value)
	{
		this.RequiresConfirmation = value;
		return this;
	}

	public IInfoScreensApi.IBasicInfoScreenRoute.IAction SetControllerKeybind(Btn? value)
	{
		this.ControllerKeybind = value;
		return this;
	}

	public IInfoScreensApi.IBasicInfoScreenRoute.IAction SetAction(Action<IInfoScreensApi.IBasicInfoScreenRoute.IAction.IArgs> value)
	{
		this.Action = value;
		return this;
	}
}

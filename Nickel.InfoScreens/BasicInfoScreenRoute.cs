using daisyowl.text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel.InfoScreens;

public sealed class BasicInfoScreenRoute : Route, IInfoScreensApi.IBasicInfoScreenRoute, OnMouseDown, OnInputPhase
{
	private static readonly UK ActionKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
	
	public Route AsRoute
		=> this;

	public Route? RouteOverride { get; set; }

	public IList<IInfoScreensApi.IBasicInfoScreenRoute.IParagraph> Paragraphs { get; set; } = [];

	public IList<IInfoScreensApi.IBasicInfoScreenRoute.IAction> Actions { get; set; } = [];

	private int? ConfirmingActionIndex;
	private double ConfirmingTime;
	
	public override bool TryCloseSubRoute(G g, Route r, object? arg)
	{
		if (this.RouteOverride?.TryCloseSubRoute(g, r, arg) ?? false)
			return true;
		
		if (r == this.RouteOverride)
		{
			r.OnExit(g.state);
			this.RouteOverride = null;
			return true;
		}
		
		return base.TryCloseSubRoute(g, r, arg);
	}

	public override void Render(G g)
	{
		if (this.RouteOverride is not null)
		{
			this.RouteOverride.Render(g);
			return;
		}
		
		const int sectionSpacing = 16;
		const int paragraphSpacing = 8;
		const int actionWidth = 61;
		const int actionHeight = 25;
		const int interActionSpacing = 16;

		if (this.ConfirmingActionIndex is { } actionIndex)
		{
			var action = this.Actions[actionIndex];
			var controllerButton = action.ControllerKeybind ?? Btn.A;

			if (!Input.mouseLeft && !Input.GetGpHeld(controllerButton))
			{
				this.ConfirmingActionIndex = null;
				this.ConfirmingTime = 0;
			}
			else
			{
				this.ConfirmingTime += g.dt;

				if (this.ConfirmingTime > 3)
				{
					var args = ModEntry.Instance.ArgsPool.Get<BasicInfoScreenRouteActionArgs>();
					try
					{
						args.G = g;
						args.Route = this;
						action.Action(args);
					}
					finally
					{
						ModEntry.Instance.ArgsPool.Return(args);
					}
				}
			}
		}
		
		#region Sizing
		var totalHeight = 0;

		for (var i = 0; i < this.Paragraphs.Count; i++)
		{
			if (i != 0)
				totalHeight += paragraphSpacing;
			var paragraph = this.Paragraphs[i];
			totalHeight += (int)Draw.Text(paragraph.Text, 0, 0, paragraph.Font, maxWidth: paragraph.MaxWidth, dontDraw: true).h;
		}

		if (this.Actions.Count != 0)
		{
			if (totalHeight > 0)
				totalHeight += sectionSpacing;
			totalHeight += actionHeight;
		}
		#endregion

		#region Rendering
		Draw.Fill(Colors.black);
		
		var topY = g.mg.PIX_H / 2 - totalHeight / 2;
		var offsetY = 0;
		
		for (var i = 0; i < this.Paragraphs.Count; i++)
		{
			if (i != 0)
				offsetY += paragraphSpacing;
			var paragraph = this.Paragraphs[i];
			offsetY += (int)Draw.Text(paragraph.Text, g.mg.PIX_W / 2, topY + offsetY, paragraph.Font, paragraph.Color, maxWidth: paragraph.MaxWidth, align: TAlign.Center).h;
		}

		if (this.Actions.Count != 0)
		{
			if (offsetY > 0)
				offsetY += sectionSpacing;

			var totalActionsWidth = actionWidth * this.Actions.Count + (interActionSpacing * (this.Actions.Count - 1));

			for (var i = 0; i < this.Actions.Count; i++)
			{
				var action = this.Actions[i];
				var text = action.Title;

				if (this.ConfirmingActionIndex == i)
				{
					var confirmingPercent = this.ConfirmingTime / 3;
					var confirmingFullText = ModEntry.Instance.Localizations.Localize(["actionConfirmationText"]);
					var confirmingPartialText = confirmingFullText[..(int)Math.Ceiling(Math.Min(confirmingPercent, 1) * confirmingFullText.Length)];
					text = $"{text}\n{confirmingPartialText}";
				}
				
				SharedArt.ButtonText(
					g,
					new Vec(g.mg.PIX_W / 2 - totalActionsWidth / 2 + (actionWidth + interActionSpacing) * i, topY + offsetY),
					new(ActionKey, i),
					text,
					textColor: action.Color,
					boxColor: action.Color,
					onMouseDown: this,
					autoFocus: true,
					platformButtonHint: action.ControllerKeybind
				);
			}
		}
		#endregion
	}

	public void OnMouseDown(G g, Box b)
	{
		if (b.key?.k != ActionKey)
			return;
		
		this.InvokeAction(g, b.key.Value.v);
	}

	public void OnInputPhase(G g, Box b)
	{
		if (this.ConfirmingActionIndex is not { } actionIndex)
			return;
		
		var action = this.Actions[actionIndex];
		if (action.ControllerKeybind is null)
			return;
		if (!Input.GetGpDown(action.ControllerKeybind.Value))
			return;
		
		this.InvokeAction(g, actionIndex);
	}

	private void InvokeAction(G g, int actionIndex)
	{
		var action = this.Actions[actionIndex];
		if (action.RequiresConfirmation)
		{
			this.ConfirmingActionIndex = actionIndex;
			return;
		}

		var args = ModEntry.Instance.ArgsPool.Get<BasicInfoScreenRouteActionArgs>();
		try
		{
			args.G = g;
			args.Route = this;
			action.Action(args);
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}

	public IInfoScreensApi.IBasicInfoScreenRoute SetRouteOverride(Route? value)
	{
		this.RouteOverride = value;
		return this;
	}

	public IInfoScreensApi.IBasicInfoScreenRoute SetParagraphs(IReadOnlyList<IInfoScreensApi.IBasicInfoScreenRoute.IParagraph> value)
	{
		this.Paragraphs = value.ToList();
		return this;
	}

	public IInfoScreensApi.IBasicInfoScreenRoute SetActions(IReadOnlyList<IInfoScreensApi.IBasicInfoScreenRoute.IAction> value)
	{
		this.Actions = value.ToList();
		return this;
	}
}

using daisyowl.text;
using System.Collections.Generic;
using System.Linq;

namespace Nickel.InfoScreens;

public sealed class BasicInfoScreenRoute : Route, IInfoScreensApi.IBasicInfoScreenRoute, OnMouseDown
{
	private static readonly UK ActionKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
	
	public Route AsRoute
		=> this;

	public IList<IInfoScreensApi.IBasicInfoScreenRoute.IParagraph> Paragraphs { get; set; } = [];

	public IList<IInfoScreensApi.IBasicInfoScreenRoute.IAction> Actions { get; set; } = [];

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

	public override void Render(G g)
	{
		const int sectionSpacing = 16;
		const int paragraphSpacing = 8;
		const int actionWidth = 61;
		const int actionHeight = 25;
		const int interActionSpacing = 16;
		
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
			offsetY += (int)Draw.Text(paragraph.Text, g.mg.PIX_W / 2, topY + offsetY, paragraph.Font, maxWidth: paragraph.MaxWidth, align: TAlign.Center).h;
		}

		if (this.Actions.Count != 0)
		{
			if (offsetY > 0)
				offsetY += sectionSpacing;

			var totalActionsWidth = actionWidth * this.Actions.Count + (interActionSpacing * (this.Actions.Count - 1));

			for (var i = 0; i < this.Actions.Count; i++)
			{
				var action = this.Actions[i];
				SharedArt.ButtonText(
					g,
					new Vec(g.mg.PIX_W / 2 - totalActionsWidth / 2 + (actionWidth + interActionSpacing) * i, topY + offsetY),
					new(ActionKey, i),
					action.Title,
					textColor: action.Color,
					boxColor: action.Color,
					onMouseDown: this,
					autoFocus: true
				);
			}
		}
		#endregion
	}

	public void OnMouseDown(G g, Box b)
	{
		if (b.key?.k != ActionKey)
			return;

		var args = ModEntry.Instance.ArgsPool.Get<BasicInfoScreenRouteActionArgs>();

		try
		{
			args.G = g;
			args.Route = this;
			
			var action = this.Actions[b.key.Value.v];
			action.Action(args);
		}
		finally
		{
			ModEntry.Instance.ArgsPool.Return(args);
		}
	}
}

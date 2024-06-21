using daisyowl.text;
using FSPRO;
using System;

namespace Nickel.ModSettings;

public sealed class ButtonModSetting : ModSetting, OnMouseDown
{
	public required Func<string> Title { get; init; }
	public Func<string?>? ValueText { get; init; }
	public required Action<G, ModSettingsRoute> OnClick { get; init; }

	public override Vec? Render(G g, Box box, bool dontDraw)
	{
		if (!dontDraw)
		{
			if (box.IsHover())
				Draw.Rect(box.rect.x, box.rect.y, box.rect.w, box.rect.h, Colors.menuHighlightBox.gain(0.5), BlendMode.Screen);
			box.onMouseDown = this;

			var textColor = box.IsHover() ? Colors.textChoiceHoverActive : Colors.textMain;
			var valueText = this.ValueText?.Invoke();

			Draw.Text(this.Title(), box.rect.x + 10, box.rect.y + 5, DB.thicket, textColor);
			Draw.Text(valueText ?? "", box.rect.x2 - 10, box.rect.y + 5, DB.thicket, textColor, align: TAlign.Right);
		}

		return new(box.rect.w, 20);
	}

	public void OnMouseDown(G g, Box b)
	{
		Audio.Play(Event.Click);

		if (b.key == this.Key)
			this.OnClick(g, this.CurrentRoute);
	}
}

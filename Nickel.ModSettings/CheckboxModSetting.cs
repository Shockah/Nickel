using FSPRO;
using System;

namespace Nickel.ModSettings;

public sealed class CheckboxModSetting : BaseModSetting, OnMouseDown
{
	public UIKey CheckboxKey { get; private set; }
	public required Func<string> Title { get; init; }
	public required Func<bool> Getter { get; init; }
	public required Action<bool> Setter { get; init; }

	public override void Prepare(G g, IModSettingsApi.IModSettingsRoute route, Func<UIKey> keyGenerator)
	{
		base.Prepare(g, route, keyGenerator);

		if (this.CheckboxKey == 0)
			this.CheckboxKey = keyGenerator();
	}

	public override Vec? Render(G g, Box box, bool dontDraw)
	{
		if (!dontDraw)
		{
			if (box.IsHover())
				Draw.Rect(box.rect.x, box.rect.y, box.rect.w, box.rect.h, Colors.menuHighlightBox.gain(0.5), BlendMode.Screen);

			var textColor = box.IsHover() ? Colors.textChoiceHoverActive : Colors.textMain;

			Draw.Text(this.Title(), box.rect.x + 10, box.rect.y + 5, DB.thicket, textColor);
			SharedArt.CheckboxBig(g, new Vec(box.rect.x2 - 10 - 15), this.CheckboxKey, this.Getter(), boxColor: Colors.buttonBoxNormal, onMouseDown: this);
		}

		return new(box.rect.w, 20);
	}

	public void OnMouseDown(G g, Box b)
	{
		Audio.Play(Event.Click);

		if (b.key == this.Key || b.key == this.CheckboxKey)
			this.Setter(!this.Getter());
	}
}

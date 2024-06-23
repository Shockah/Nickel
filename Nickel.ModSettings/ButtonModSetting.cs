using daisyowl.text;
using FSPRO;
using System;
using System.Collections.Generic;

namespace Nickel.ModSettings;

public sealed class ButtonModSetting : BaseModSetting, OnMouseDown, IModSettingsApi.IButtonModSetting
{
	public required Func<string> Title { get; set; }
	public Func<string?>? ValueText { get; set; }
	public required Action<G, IModSettingsApi.IModSettingsRoute> OnClick { get; set; }
	public Func<IEnumerable<Tooltip>>? Tooltips { get; set; }

	IModSettingsApi.IButtonModSetting IModSettingsApi.IButtonModSetting.SetTitle(Func<string> value)
	{
		this.Title = value;
		return this;
	}

	IModSettingsApi.IButtonModSetting IModSettingsApi.IButtonModSetting.SetValueText(Func<string?>? value)
	{
		this.ValueText = value;
		return this;
	}

	IModSettingsApi.IButtonModSetting IModSettingsApi.IButtonModSetting.SetOnClick(Action<G, IModSettingsApi.IModSettingsRoute> value)
	{
		this.OnClick = value;
		return this;
	}

	IModSettingsApi.IButtonModSetting IModSettingsApi.IButtonModSetting.SetTooltips(Func<IEnumerable<Tooltip>>? value)
	{
		this.Tooltips = value;
		return this;
	}

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

			if (box.IsHover() && this.Tooltips is { } tooltips)
				g.tooltips.Add(new Vec(box.rect.x2 - Tooltip.WIDTH, box.rect.y2), tooltips());
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

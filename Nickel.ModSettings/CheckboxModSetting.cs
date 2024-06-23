using FSPRO;
using System;
using System.Collections.Generic;

namespace Nickel.ModSettings;

public sealed class CheckboxModSetting : BaseModSetting, OnMouseDown, IModSettingsApi.ICheckboxModSetting
{
	public required Func<string> Title { get; set; }
	public required Func<bool> Getter { get; set; }
	public required Action<bool> Setter { get; set; }
	public Func<IEnumerable<Tooltip>>? Tooltips { get; set; }

	private UIKey CheckboxKey;

	public CheckboxModSetting() : base()
	{
		this.OnMenuOpen += (g, route, keyGenerator) =>
		{
			if (this.CheckboxKey == 0)
				this.CheckboxKey = keyGenerator();
		};
	}

	IModSettingsApi.ICheckboxModSetting IModSettingsApi.ICheckboxModSetting.SetTitle(Func<string> value)
	{
		this.Title = value;
		return this;
	}

	IModSettingsApi.ICheckboxModSetting IModSettingsApi.ICheckboxModSetting.SetGetter(Func<bool> value)
	{
		this.Getter = value;
		return this;
	}

	IModSettingsApi.ICheckboxModSetting IModSettingsApi.ICheckboxModSetting.SetSetter(Action<bool> value)
	{
		this.Setter = value;
		return this;
	}

	IModSettingsApi.ICheckboxModSetting IModSettingsApi.ICheckboxModSetting.SetTooltips(Func<IEnumerable<Tooltip>>? value)
	{
		this.Tooltips = value;
		return this;
	}

	public override Vec? Render(G g, Box box, bool dontDraw)
	{
		if (!dontDraw)
		{
			var isHover = box.IsHover() || g.hoverKey == this.CheckboxKey;
			if (isHover)
				Draw.Rect(box.rect.x, box.rect.y, box.rect.w, box.rect.h, Colors.menuHighlightBox.gain(0.5), BlendMode.Screen);

			var textColor = isHover ? Colors.textChoiceHoverActive : Colors.textMain;

			Draw.Text(this.Title(), box.rect.x + 10, box.rect.y + 5, DB.thicket, textColor);
			SharedArt.CheckboxBig(g, new Vec(box.rect.w - 10 - 15, 1), this.CheckboxKey, this.Getter(), boxColor: Colors.buttonBoxNormal, onMouseDown: this);

			if (isHover && this.Tooltips is { } tooltips)
				g.tooltips.Add(new Vec(box.rect.x2 - Tooltip.WIDTH, box.rect.y2), tooltips());
		}

		box.onMouseDown = this;
		return new(box.rect.w, 20);
	}

	public void OnMouseDown(G g, Box b)
	{
		Audio.Play(Event.Click);

		if (b.key == this.Key || b.key == this.CheckboxKey)
			this.Setter(!this.Getter());
	}
}

using FSPRO;
using System;
using System.Collections.Generic;

namespace Nickel.ModSettings;

public sealed class CheckboxModSetting : BaseModSetting, OnMouseDown, IModSettingsApi.ICheckboxModSetting
{
	private const double PreferredHeight = 20;

	public required Func<string> Title { get; set; }
	public required Func<bool> Getter { get; set; }
	public required Action<G, IModSettingsApi.IModSettingsRoute, bool> Setter { get; set; }
	public Func<IEnumerable<Tooltip>>? Tooltips { get; set; }

	private UIKey CheckboxKey;

	public CheckboxModSetting()
	{
		this.OnMenuOpen += (_, _) =>
		{
			if (this.CheckboxKey == 0)
				this.CheckboxKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
		};
	}

	~CheckboxModSetting()
	{
		if (this.CheckboxKey != 0)
			ModEntry.Instance.Helper.Utilities.FreeEnumCase(this.CheckboxKey.k);
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

	IModSettingsApi.ICheckboxModSetting IModSettingsApi.ICheckboxModSetting.SetSetter(Action<G, IModSettingsApi.IModSettingsRoute, bool> value)
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
		if (box.key is not null)
		{
			box.autoFocus = true;
			box.onMouseDown = this;
		}
		
		if (!dontDraw)
		{
			var isHover = (box.key is not null && box.IsHover()) || g.hoverKey == this.CheckboxKey;
			if (isHover)
			{
				Draw.Rect(box.rect.x, box.rect.y, box.rect.w, box.rect.h, Colors.menuHighlightBox.gain(0.5), BlendMode.Screen);
				if (this.Tooltips is { } tooltips)
					g.tooltips.Add(new Vec(box.rect.x2 - Tooltip.WIDTH, box.rect.y2), tooltips());
			}
				

			var textColor = isHover ? Colors.textChoiceHoverActive : Colors.textMain;

			Draw.Text(this.Title(), box.rect.x + 10, box.rect.y + 5 + (int)((box.rect.h - PreferredHeight) / 2), DB.thicket, textColor);
			SharedArt.CheckboxBig(g, new Vec(box.rect.w - 10 - 15, 1 + (int)((box.rect.h - PreferredHeight) / 2)), this.CheckboxKey, this.Getter(), boxColor: Colors.buttonBoxNormal, onMouseDown: this);
		}

		return new(box.rect.w, dontDraw ? PreferredHeight : box.rect.h);
	}

	public void OnMouseDown(G g, Box b)
	{
		Audio.Play(Event.Click);

		if (b.key == this.Key || b.key == this.CheckboxKey)
			this.Setter(g, this.CurrentRoute, !this.Getter());
	}
}

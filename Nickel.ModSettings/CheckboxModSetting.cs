using daisyowl.text;
using FSPRO;
using System;
using System.Collections.Generic;

namespace Nickel.ModSettings;

public sealed class CheckboxModSetting : BaseModSetting, OnMouseDown, IModSettingsApi.ICheckboxModSetting
{
	private const int PreferredHeight = 20;

	public required Func<string> Title { get; set; }
	public Func<Font?>? TitleFont { get; set; }
	public Func<bool, string?>? ValueText { get; set; }
	public Func<bool, Font?>? ValueTextFont { get; set; }
	public required Func<bool> Getter { get; set; }
	public required Action<G, IModSettingsApi.IModSettingsRoute, bool> Setter { get; set; }
	public Func<IEnumerable<Tooltip>>? Tooltips { get; set; }
	public int Height { get; set; } = PreferredHeight;

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

	IModSettingsApi.ICheckboxModSetting IModSettingsApi.ICheckboxModSetting.SetTitleFont(Func<Font?>? value)
	{
		this.TitleFont = value;
		return this;
	}

	IModSettingsApi.ICheckboxModSetting IModSettingsApi.ICheckboxModSetting.SetValueText(Func<bool, string?>? value)
	{
		this.ValueText = value;
		return this;
	}

	IModSettingsApi.ICheckboxModSetting IModSettingsApi.ICheckboxModSetting.SetValueTextFont(Func<bool, Font?>? value)
	{
		this.ValueTextFont = value;
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

	IModSettingsApi.ICheckboxModSetting IModSettingsApi.ICheckboxModSetting.SetHeight(int value)
	{
		this.Height = value;
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
			var isHover = (box.key is not null && box.IsHover());
			if (isHover)
			{
				Draw.Rect(box.rect.x, box.rect.y, box.rect.w, box.rect.h, Colors.menuHighlightBox.gain(0.5), BlendMode.Screen);
				if (this.Tooltips is { } tooltips)
					g.tooltips.Add(new Vec(box.rect.x2 - Tooltip.WIDTH, box.rect.y2), tooltips());
			}

			var value = this.Getter();
			
			var textColor = isHover ? Colors.textChoiceHoverActive : Colors.textMain;
			var titleFont = this.TitleFont?.Invoke() ?? DB.thicket;
			var titleRect = Draw.Text(this.Title(), 0, 0, titleFont, dontDraw: true);
			Draw.Text(this.Title(), box.rect.x + 10, (int)box.rect.Center().y - (int)(titleRect.h / 2) - 1, titleFont, textColor);

			if (this.ValueText?.Invoke(value) is { } valueText && !string.IsNullOrEmpty(valueText))
			{
				var valueTextFont = this.ValueTextFont?.Invoke(value) ?? DB.thicket;
				var valueRect = Draw.Text(valueText ?? "", 0, 0, valueTextFont, dontDraw: true);
				Draw.Text(valueText ?? "", box.rect.x2 - 10, (int)box.rect.Center().y - (int)(valueRect.h / 2) - 1, valueTextFont, textColor, align: TAlign.Right, dontDraw: dontDraw);
			}
			else
			{
				SharedArt.CheckboxBig(g, new Vec(box.rect.w - 10 - 15, (int)((box.rect.h - 17) / 2)), this.CheckboxKey, value, boxColor: Colors.buttonBoxNormal, noHover: true);
			}
		}

		return new(box.rect.w, dontDraw ? this.Height : box.rect.h);
	}

	public void OnMouseDown(G g, Box b)
	{
		Audio.Play(Event.Click);

		if (b.key == this.Key || b.key == this.CheckboxKey)
			this.Setter(g, this.CurrentRoute, !this.Getter());
	}
}

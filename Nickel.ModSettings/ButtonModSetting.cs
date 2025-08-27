using daisyowl.text;
using FSPRO;
using System;
using System.Collections.Generic;

namespace Nickel.ModSettings;

public sealed class ButtonModSetting : BaseModSetting, OnMouseDown, IModSettingsApi.IButtonModSetting
{
	private const int PreferredHeight = 20;

	public required Func<string> Title { get; set; }
	public Func<Font?>? TitleFont { get; set; }
	public Func<string?>? ValueText { get; set; }
	public Func<Font?>? ValueTextFont { get; set; }
	public required Action<G, IModSettingsApi.IModSettingsRoute> OnClick { get; set; }
	public Func<IEnumerable<Tooltip>>? Tooltips { get; set; }
	public IModSettingsApi.HorizontalAlignment TitleHorizontalAlignment { get; set; } = IModSettingsApi.HorizontalAlignment.Left;
	public int Height { get; set; } = PreferredHeight;
	public int Spacing { get; set; } = 10;

	IModSettingsApi.IButtonModSetting IModSettingsApi.IButtonModSetting.SetTitle(Func<string> value)
	{
		this.Title = value;
		return this;
	}

	IModSettingsApi.IButtonModSetting IModSettingsApi.IButtonModSetting.SetTitleFont(Func<Font?>? value)
	{
		this.TitleFont = value;
		return this;
	}

	IModSettingsApi.IButtonModSetting IModSettingsApi.IButtonModSetting.SetValueText(Func<string?>? value)
	{
		this.ValueText = value;
		return this;
	}

	IModSettingsApi.IButtonModSetting IModSettingsApi.IButtonModSetting.SetValueTextFont(Func<Font?>? value)
	{
		this.ValueTextFont = value;
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

	IModSettingsApi.IButtonModSetting IModSettingsApi.IButtonModSetting.SetTitleHorizontalAlignment(IModSettingsApi.HorizontalAlignment value)
	{
		this.TitleHorizontalAlignment = value;
		return this;
	}

	IModSettingsApi.IButtonModSetting IModSettingsApi.IButtonModSetting.SetHeight(int value)
	{
		this.Height = value;
		return this;
	}

	IModSettingsApi.IButtonModSetting IModSettingsApi.IButtonModSetting.SetSpacing(int value)
	{
		this.Spacing = value;
		return this;
	}

	public override Vec? Render(G g, Box box, bool dontDraw)
	{
		if (box.key is not null)
		{
			box.autoFocus = true;
			box.onMouseDown = this;
		}

		var isHover = box.key is not null && box.IsHover();
		if (!dontDraw && isHover)
		{
			Draw.Rect(box.rect.x, box.rect.y, box.rect.w, box.rect.h, Colors.menuHighlightBox.gain(0.5), BlendMode.Screen);
			if (this.Tooltips is { } tooltips)
				g.tooltips.Add(new Vec(box.rect.x2 - Tooltip.WIDTH, box.rect.y2), tooltips());
		}
		
		var textColor = isHover ? Colors.textChoiceHoverActive : Colors.textMain;
		var valueText = this.ValueText?.Invoke();
		
		var titleFont = this.TitleFont?.Invoke() ?? DB.thicket;
		var valueTextFont = this.ValueTextFont?.Invoke() ?? DB.thicket;
		var titleRect = Draw.Text(this.Title(), 0, 0, titleFont, dontDraw: true);
		titleRect = this.TitleHorizontalAlignment switch
		{
			IModSettingsApi.HorizontalAlignment.Left => Draw.Text(this.Title(), box.rect.x + 10, (int)box.rect.Center().y - (int)(titleRect.h / 2) - 1, titleFont, textColor, align: TAlign.Left, dontDraw: dontDraw),
			IModSettingsApi.HorizontalAlignment.Right => Draw.Text(this.Title(), box.rect.x2 - 10, (int)box.rect.Center().y - (int)(titleRect.h / 2) - 1, titleFont, textColor, align: TAlign.Right, dontDraw: dontDraw),
			_ => Draw.Text(this.Title(), box.rect.Center().x, (int)box.rect.Center().y - (int)(titleRect.h / 2) - 1, titleFont, textColor, align: TAlign.Center, dontDraw: dontDraw),
		};
		
		var valueRect = Draw.Text(valueText ?? "", 0, 0, valueTextFont, dontDraw: true);
		valueRect = Draw.Text(valueText ?? "", box.rect.x2 - 10, (int)box.rect.Center().y - (int)(valueRect.h / 2) - 1, valueTextFont, textColor, align: TAlign.Right, dontDraw: dontDraw);

		var requiredWidth = titleRect.w + (string.IsNullOrEmpty(valueText) ? 0 : valueRect.w + this.Spacing);
		return new(Math.Min(box.rect.w, requiredWidth), dontDraw ? this.Height : box.rect.h);
	}

	public void OnMouseDown(G g, Box b)
	{
		Audio.Play(Event.Click);

		if (b.key == this.Key)
			this.OnClick(g, this.CurrentRoute);
	}
}

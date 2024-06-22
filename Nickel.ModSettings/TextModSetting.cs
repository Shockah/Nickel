using daisyowl.text;
using System;

namespace Nickel.ModSettings;

public sealed class TextModSetting : BaseModSetting, IModSettingsApi.ITextModSetting
{
	public required Func<string> Text { get; set; }
	public Font Font { get; set; } = DB.pinch;
	public Color Color { get; set; } = Colors.textMain;
	public TAlign Alignment { get; set; } = TAlign.Left;
	public bool WrapText { get; set; } = true;

	IModSettingsApi.ITextModSetting IModSettingsApi.ITextModSetting.SetText(Func<string> value)
	{
		this.Text = value;
		return this;
	}

	IModSettingsApi.ITextModSetting IModSettingsApi.ITextModSetting.SetFont(Font value)
	{
		this.Font = value;
		return this;
	}

	IModSettingsApi.ITextModSetting IModSettingsApi.ITextModSetting.SetColor(Color value)
	{
		this.Color = value;
		return this;
	}

	IModSettingsApi.ITextModSetting IModSettingsApi.ITextModSetting.SetAlignment(TAlign value)
	{
		this.Alignment = value;
		return this;
	}

	IModSettingsApi.ITextModSetting IModSettingsApi.ITextModSetting.SetWrapText(bool value)
	{
		this.WrapText = value;
		return this;
	}

	public override Vec? Render(G g, Box box, bool dontDraw)
	{
		var rect = this.Alignment switch
		{
			TAlign.Left => Draw.Text(this.Text(), box.rect.x + 10, box.rect.y, this.Font, this.Color, maxWidth: this.WrapText ? box.rect.w - 20 : null, align: TAlign.Left, dontDraw: dontDraw),
			TAlign.Right => Draw.Text(this.Text(), box.rect.x2 - 10, box.rect.y, this.Font, this.Color, maxWidth: this.WrapText ? box.rect.w - 20 : null, align: TAlign.Right, dontDraw: dontDraw),
			_ => Draw.Text(this.Text(), box.rect.Center().x, box.rect.y, this.Font, this.Color, maxWidth: this.WrapText ? box.rect.w - 20 : null, align: TAlign.Center, dontDraw: dontDraw),
		};

		return new(box.rect.w, rect.h);
	}
}

using daisyowl.text;
using System;

namespace Nickel.ModSettings;

public sealed class TextModSetting : ModSetting
{
	public required Func<string> Text { get; init; }
	public TAlign Alignment { get; init; } = TAlign.Left;

	public override Vec? Render(G g, Box box, bool dontDraw)
	{
		var rect = this.Alignment switch
		{
			TAlign.Left => Draw.Text(this.Text(), box.rect.x + 10, box.rect.y, DB.pinch, Colors.textMain, maxWidth: box.rect.w - 20, align: TAlign.Left, dontDraw: dontDraw),
			TAlign.Right => Draw.Text(this.Text(), box.rect.x2 - 10, box.rect.y, DB.pinch, Colors.textMain, maxWidth: box.rect.w - 20, align: TAlign.Right, dontDraw: dontDraw),
			_ => Draw.Text(this.Text(), box.rect.Center().x, box.rect.y, DB.pinch, Colors.textMain, maxWidth: box.rect.w - 20, align: TAlign.Center, dontDraw: dontDraw),
		};

		return new(box.rect.w, rect.h);
	}
}

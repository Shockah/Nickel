using daisyowl.text;
using System;

namespace Nickel.ModSettings;

public sealed class HeaderModSetting : BaseModSetting, IModSettingsApi.IHeaderModSetting
{
	public required Func<string> Title { get; set; }
	public TAlign Alignment { get; set; } = TAlign.Center;

	IModSettingsApi.IHeaderModSetting IModSettingsApi.IHeaderModSetting.SetTitle(Func<string> value)
	{
		this.Title = value;
		return this;
	}

	IModSettingsApi.IHeaderModSetting IModSettingsApi.IHeaderModSetting.SetAlignment(TAlign value)
	{
		this.Alignment = value;
		return this;
	}

	public override Vec? Render(G g, Box box, bool dontDraw)
	{
		if (!dontDraw)
		{
			switch (this.Alignment)
			{
				case TAlign.Center:
					Draw.Text(this.Title(), box.rect.Center().x, box.rect.y + 4, DB.stapler, Colors.textMain, align: TAlign.Center);
					break;
				case TAlign.Left:
					Draw.Text(this.Title(), box.rect.x + 10, box.rect.y + 4, DB.stapler, Colors.textMain, align: TAlign.Left);
					break;
				case TAlign.Right:
					Draw.Text(this.Title(), box.rect.x2 - 10, box.rect.y + 4, DB.stapler, Colors.textMain, align: TAlign.Right);
					break;
			}
		}

		return new(box.rect.w, 28);
	}
}

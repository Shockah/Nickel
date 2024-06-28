using System;

namespace Nickel.ModSettings;

public sealed class ConditionalModSetting : BaseModSetting, IModSettingsApi.IConditionalModSetting
{
	public required IModSettingsApi.IModSetting Setting { get; set; }
	public required Func<bool> IsVisible { get; set; }

	public ConditionalModSetting()
	{
		this.OnMenuOpen += (g, route) => this.Setting?.RaiseOnMenuOpen(g, route);
		this.OnMenuClose += g => this.Setting?.RaiseOnMenuClose(g);
	}

	IModSettingsApi.IConditionalModSetting IModSettingsApi.IConditionalModSetting.SetSetting(IModSettingsApi.IModSetting value)
	{
		this.Setting = value;
		return this;
	}

	IModSettingsApi.IConditionalModSetting IModSettingsApi.IConditionalModSetting.SetVisible(Func<bool> value)
	{
		this.IsVisible = value;
		return this;
	}

	public override Vec? Render(G g, Box box, bool dontDraw)
	{
		if (!this.IsVisible())
			return null;

		g.Push(null, new Rect(box.rect.x, box.rect.y, box.rect.w, 0));
		var nullableSettingSize = this.Setting.Render(g, box, dontDraw: true);
		g.Pop();

		if (nullableSettingSize is not { } settingSize)
			return null;

		if (!dontDraw)
		{
			var childBox = g.Push(this.Setting.Key, new Rect(0, 0, box.rect.w, settingSize.y));
			this.Setting.Render(g, childBox, dontDraw: false);
			g.Pop();
		}
		return new(box.rect.w, settingSize.y);
	}
}

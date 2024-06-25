namespace Nickel.ModSettings;

public sealed class PaddingModSetting : BaseModSetting, IModSettingsApi.IPaddingModSetting
{
	public required IModSettingsApi.IModSetting Setting { get; set; }
	public required int TopPadding { get; set; }
	public required int BottomPadding { get; set; }

	public PaddingModSetting()
	{
		this.OnMenuOpen += (g, route, keyGenerator) => this.Setting?.RaiseOnMenuOpen(g, route, keyGenerator);
		this.OnMenuClose += g => this.Setting?.RaiseOnMenuClose(g);
	}

	IModSettingsApi.IPaddingModSetting IModSettingsApi.IPaddingModSetting.SetSetting(IModSettingsApi.IModSetting value)
	{
		this.Setting = value;
		return this;
	}

	IModSettingsApi.IPaddingModSetting IModSettingsApi.IPaddingModSetting.SetTopPadding(int value)
	{
		this.TopPadding = value;
		return this;
	}

	IModSettingsApi.IPaddingModSetting IModSettingsApi.IPaddingModSetting.SetBottomPadding(int value)
	{
		this.BottomPadding = value;
		return this;
	}

	public override Vec? Render(G g, Box box, bool dontDraw)
	{
		g.Push(null, new Rect(box.rect.x, box.rect.y, box.rect.w, 0));
		var nullableSettingSize = this.Setting.Render(g, box, dontDraw: true);
		g.Pop();

		if (nullableSettingSize is not { } settingSize)
			return null;

		if (!dontDraw)
		{
			var childBox = g.Push(this.Setting.Key, new Rect(0, 0, box.rect.w, settingSize.y + this.TopPadding + this.BottomPadding));
			var contentBox = g.Push(null, new Rect(0, this.TopPadding, box.rect.w, settingSize.y));
			contentBox._isHover = childBox._isHover;
			this.Setting.Render(g, contentBox, dontDraw: false);
			childBox._isHover_listen = contentBox._isHover_listen;
			g.Pop();
			g.Pop();
		}
		return new(box.rect.w, settingSize.y + this.TopPadding + this.BottomPadding);
	}
}

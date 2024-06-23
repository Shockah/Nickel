using System.Collections.Generic;

namespace Nickel.ModSettings;

public sealed class ListModSetting : BaseModSetting, IModSettingsApi.IListModSetting
{
	public required IList<IModSettingsApi.IModSetting> Settings { get; set; }
	public IModSettingsApi.IModSetting? EmptySetting { get; set; }
	public int Spacing { get; set; } = 0;

	public ListModSetting() : base()
	{
		this.OnMenuOpen += (g, route, keyGenerator) =>
		{
			if (this.Settings is { } settings)
				foreach (var setting in settings)
					setting.RaiseOnMenuOpen(g, route, keyGenerator);
			this.EmptySetting?.RaiseOnMenuOpen(g, route, keyGenerator);
		};
		this.OnMenuClose += g =>
		{
			if (this.Settings is { } settings)
				foreach (var setting in settings)
					setting.RaiseOnMenuClose(g);
			this.EmptySetting?.RaiseOnMenuClose(g);
		};
	}

	IModSettingsApi.IListModSetting IModSettingsApi.IListModSetting.SetSettings(IList<IModSettingsApi.IModSetting> value)
	{
		this.Settings = value;
		return this;
	}

	IModSettingsApi.IListModSetting IModSettingsApi.IListModSetting.SetEmptySetting(IModSettingsApi.IModSetting? value)
	{
		this.EmptySetting = value;
		return this;
	}

	IModSettingsApi.IListModSetting IModSettingsApi.IListModSetting.SetSpacing(int value)
	{
		this.Spacing = value;
		return this;
	}

	public override Vec? Render(G g, Box box, bool dontDraw)
	{
		var renderingAnyElement = false;
		var totalHeight = 0.0;

		foreach (var setting in this.Settings)
		{
			g.Push(null, new Rect(box.rect.x, box.rect.y + totalHeight, box.rect.w, 0));
			var nullableSettingSize = setting.Render(g, box, dontDraw: true);
			g.Pop();

			if (nullableSettingSize is not { } settingSize)
				continue;

			if (renderingAnyElement)
				totalHeight += this.Spacing;
			totalHeight += settingSize.y;
			renderingAnyElement = true;

			if (!dontDraw)
			{
				var childBox = g.Push(setting.Key, new Rect(0, totalHeight - settingSize.y, box.rect.w, settingSize.y));
				setting.Render(g, childBox, dontDraw: false);
				g.Pop();
			}
		}

		if (!renderingAnyElement && this.EmptySetting is { } emptySetting)
		{
			g.Push(null, new Rect(box.rect.x, box.rect.y + totalHeight, box.rect.w, 0));
			var nullableSettingSize = emptySetting.Render(g, box, dontDraw: true);
			g.Pop();

			if (nullableSettingSize is not { } settingSize)
				return null;

			if (!dontDraw)
			{
				var childBox = g.Push(emptySetting.Key, new Rect(0, 0, box.rect.w, settingSize.y));
				emptySetting.Render(g, childBox, dontDraw: false);
				g.Pop();
			}
			return new(box.rect.w, settingSize.y);
		}

		return renderingAnyElement ? new(box.rect.w, totalHeight) : null;
	}
}

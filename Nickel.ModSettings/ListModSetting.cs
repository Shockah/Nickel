using System;
using System.Collections.Generic;

namespace Nickel.ModSettings;

public sealed class ListModSetting : BaseModSetting, IModSettingsApi.IListModSetting
{
	public required IList<IModSettingsApi.IModSetting> Settings { get; set; }
	public IModSettingsApi.IModSetting? EmptySetting { get; set; }
	public int Spacing { get; set; } = 0;

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

	public override void Prepare(G g, IModSettingsApi.IModSettingsRoute route, Func<UIKey> keyGenerator)
	{
		base.Prepare(g, route, keyGenerator);
		foreach (var setting in this.Settings)
			setting.Prepare(g, route, keyGenerator);
		this.EmptySetting?.Prepare(g, route, keyGenerator);
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
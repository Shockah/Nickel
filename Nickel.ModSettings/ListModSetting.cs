using System;
using System.Collections.Generic;

namespace Nickel.ModSettings;

public sealed class ListModSetting : ModSetting
{
	public required List<ModSetting> Settings { get; init; }
	public ModSetting? EmptySetting { get; init; }
	public int Spacing = 0;

	public override void Initialize(G g, ModSettingsRoute route, Func<UIKey> keyGenerator)
	{
		base.Initialize(g, route, keyGenerator);
		foreach (var setting in this.Settings)
			setting.Initialize(g, route, keyGenerator);
		this.EmptySetting?.Initialize(g, route, keyGenerator);
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

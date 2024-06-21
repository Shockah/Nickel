using System;

namespace Nickel.ModSettings;

public sealed class ConditionalModSetting : ModSetting
{
	public required ModSetting Setting { get; init; }
	public required Func<bool> IsVisible { get; init; }

	public override void Initialize(G g, ModSettingsRoute route, Func<UIKey> keyGenerator)
	{
		base.Initialize(g, route, keyGenerator);
		this.Setting.Initialize(g, route, keyGenerator);
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

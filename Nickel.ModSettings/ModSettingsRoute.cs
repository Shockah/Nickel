using Newtonsoft.Json;
using System;

namespace Nickel.ModSettings;

public sealed class ModSettingsRoute : Route
{
	private static int NextUK = 500_001;

	[JsonIgnore]
	public required ModSetting Setting;

	public Route? Subroute;

	[JsonProperty]
	private double Scroll;

	[JsonProperty]
	private double ScrollTarget;

	public override void Render(G g)
	{
		if (this.Setting is null)
		{
			// most likely deserialized; settings are not serializable; aborting
			g.CloseRoute(this);
			return;
		}
		if (this.Subroute is { } subroute)
		{
			subroute.Render(g);
			return;
		}
		this.Setting.Initialize(g, this, () => (UK)NextUK++);

		Draw.Fill(Colors.black);

		var rect = new Rect((MG.inst.PIX_W - SettingsRoute.WIDTH) / 2, 10, SettingsRoute.WIDTH, 0);
		var box = g.Push(null, rect);
		var nullableSettingSize = this.Setting.Render(g, box, dontDraw: true);
		g.Pop();

		if (nullableSettingSize is not { } settingSize)
			return;

		var preferredHeightOnScreen = MG.inst.PIX_H - 20;
		var maxScroll = (int)Math.Max(settingSize.y - preferredHeightOnScreen, 0);
		ScrollUtils.ReadScrollInputAndUpdate(g.dt, maxScroll, ref this.Scroll, ref this.ScrollTarget);

		rect = new Rect((MG.inst.PIX_W - SettingsRoute.WIDTH) / 2, 10 - this.Scroll, SettingsRoute.WIDTH, settingSize.y);
		box = g.Push(this.Setting.Key, rect);
		this.Setting.Render(g, box, dontDraw: false);
		g.Pop();
	}

	public override bool TryCloseSubRoute(G g, Route r, object? arg)
	{
		if (r == this.Subroute)
		{
			this.Subroute = null;
			return true;
		}
		return this.Subroute?.TryCloseSubRoute(g, r, arg) ?? false;
	}
}

using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;

namespace Nickel.ModSettings;

public sealed class ModSettingsRoute : Route, OnInputPhase, IModSettingsApi.IModSettingsRoute
{
	private static int NextUK = 500_001;

	[JsonIgnore]
	public required IModSettingsApi.IModSetting Setting;

	public Route? Subroute;

	[JsonProperty]
	private double Scroll;

	[JsonProperty]
	private double ScrollTarget;

	[JsonIgnore]
	public Route AsRoute
		=> this;

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

		g.Push(onInputPhase: this);
		this.Setting.Prepare(g, this, () => (UK)NextUK++);

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
		g.Pop();
	}

	public void OnInputPhase(G g, Box b)
	{
		if (Input.GetGpDown(Btn.B) || Input.GetKeyDown(Keys.Escape))
			g.CloseRoute(this);
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

	public void CloseRoute(G g)
		=> g.CloseRoute(this);

	public void OpenSubroute(G g, Route route)
	{
		if (this.Subroute is { } subroute)
			g.CloseRoute(subroute);
		this.Subroute = route;
	}
}

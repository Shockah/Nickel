using FSPRO;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace Nickel.ModSettings;

public sealed class ModSettingsRoute : Route, OnInputPhase, IModSettingsApi.IModSettingsRoute
{
	private static readonly UK WarningPopupKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

	[JsonProperty]
	public Route? Subroute;

	[JsonIgnore]
	public required IModSettingsApi.IModSetting Setting;

	[JsonIgnore]
	private double Scroll;

	[JsonIgnore]
	private double ScrollTarget;

	[JsonIgnore]
	private bool RaisedOnOpen;

	[JsonIgnore]
	private UIKey? LastGpKey;

	[JsonIgnore]
	public (string Text, double Time)? Warning;

	[JsonIgnore]
	public Route AsRoute
		=> this;

	public override void Render(G g)
	{
		if (this.Subroute is { } subroute)
		{
			subroute.Render(g);
			return;
		}
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (this.Setting is null)
		{
			// most likely deserialized; settings are not serializable; aborting
			g.CloseRoute(this);
			return;
		}

		g.Push(onInputPhase: this);
		if (!this.RaisedOnOpen)
		{
			this.Setting.RaiseOnMenuOpen(MG.inst.g, this);
			this.RaisedOnOpen = true;
		}

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

		rect = new Rect((MG.inst.PIX_W - SettingsRoute.WIDTH) / 2, 10 + (int)this.Scroll, SettingsRoute.WIDTH, settingSize.y);
		box = g.Push(this.Setting.Key, rect);
		this.Setting.Render(g, box, dontDraw: false);

		g.Pop();

		if (Input.gamepadIsActiveInput)
		{
			if (Input.currentGpKey != this.LastGpKey && Input.currentGpKey is { } currentGpKey && g.boxes.FirstOrDefault(b => b.key == currentGpKey) is { } currentGpBox)
			{
				var scrolled = currentGpBox.rect;
				var target = scrolled;

				if (target.y2 > MG.inst.PIX_H - 60)
					target.y = MG.inst.PIX_H - 60 - target.h;
				if (target.y < 60)
					target.y = 60;

				this.ScrollTarget = target.y + (int)this.Scroll - scrolled.y;
				this.ScrollTarget = Math.Clamp(this.ScrollTarget, -maxScroll, 0);
			}
			this.LastGpKey = g.hoverKey;
		}

		if (this.Warning is { } warning)
		{
			SharedArt.WarningPopup(g, WarningPopupKey, warning.Text, new Vec(240, 65));

			var warningTime = Math.Max(0, warning.Time - g.dt);
			this.Warning = warningTime <= 0 ? null : (Text: warning.Text, Time: warningTime);
		}

		g.Pop();
	}

	public override void OnExit(State s)
	{
		this.RaisedOnOpen = false;
		this.Setting.RaiseOnMenuClose(MG.inst.g);
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
			r.OnExit(g.state);
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

	public void ShowWarning(string text, double time)
	{
		this.Warning = (Text: text, Time: time);
		Audio.Play(Event.ZeroEnergy);
	}
}

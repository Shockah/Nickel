using FSPRO;
using System;
using System.Collections.Generic;
using TextCopy;

namespace Nickel.UpdateChecks;

public sealed class TokenModSetting : IUpdateChecksApi.ITokenModSetting
{
	public UIKey Key { get; private set; }
	public event IModSettingsApi.OnMenuOpen? OnMenuOpen;
	public event IModSettingsApi.OnMenuClose? OnMenuClose;

	public required Func<string> Title { get; set; }
	public required Func<bool> HasValue { get; set; }
	public Action<G, IModSettingsApi.IModSettingsRoute, string?>? PasteAction { get; set; }
	public required Action<G, IModSettingsApi.IModSettingsRoute> SetupAction { get; set; }
	public Func<IEnumerable<Tooltip>>? BaseTooltips { get; set; }
	public Func<IEnumerable<Tooltip>>? PasteTooltips { get; set; }
	public Func<IEnumerable<Tooltip>>? SetupTooltips { get; set; }

	private UIKey PasteKey;
	private UIKey SetupKey;
	private UIKey CheckboxKey;
	private IModSettingsApi.IModSettingsRoute CurrentRoute = null!;

	public TokenModSetting()
	{
		this.OnMenuOpen += (_, route) =>
		{
			if (this.Key == 0)
				this.Key = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
			if (this.PasteKey == 0)
				this.PasteKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
			if (this.SetupKey == 0)
				this.SetupKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
			if (this.CheckboxKey == 0)
				this.CheckboxKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
			this.CurrentRoute = route;
		};
	}

	IUpdateChecksApi.ITokenModSetting IUpdateChecksApi.ITokenModSetting.SetTitle(Func<string> value)
	{
		this.Title = value;
		return this;
	}

	IUpdateChecksApi.ITokenModSetting IUpdateChecksApi.ITokenModSetting.SetHasValue(Func<bool> value)
	{
		this.HasValue = value;
		return this;
	}

	IUpdateChecksApi.ITokenModSetting IUpdateChecksApi.ITokenModSetting.SetPasteAction(Action<G, IModSettingsApi.IModSettingsRoute, string?>? value)
	{
		this.PasteAction = value;
		return this;
	}

	IUpdateChecksApi.ITokenModSetting IUpdateChecksApi.ITokenModSetting.SetSetupAction(Action<G, IModSettingsApi.IModSettingsRoute> value)
	{
		this.SetupAction = value;
		return this;
	}

	IUpdateChecksApi.ITokenModSetting IUpdateChecksApi.ITokenModSetting.SetBaseTooltips(Func<IEnumerable<Tooltip>>? value)
	{
		this.BaseTooltips = value;
		return this;
	}

	IUpdateChecksApi.ITokenModSetting IUpdateChecksApi.ITokenModSetting.SetPasteTooltips(Func<IEnumerable<Tooltip>>? value)
	{
		this.PasteTooltips = value;
		return this;
	}

	IUpdateChecksApi.ITokenModSetting IUpdateChecksApi.ITokenModSetting.SetSetupTooltips(Func<IEnumerable<Tooltip>>? value)
	{
		this.SetupTooltips = value;
		return this;
	}

	public void RaiseOnMenuOpen(G g, IModSettingsApi.IModSettingsRoute route)
		=> this.OnMenuOpen?.Invoke(g, route);

	public void RaiseOnMenuClose(G g)
		=> this.OnMenuClose?.Invoke(g);

	public Vec? Render(G g, Box box, bool dontDraw)
	{
		if (!dontDraw)
		{
			box.autoFocus = true;

			var isHover = box.IsHover() || g.hoverKey == this.PasteKey || g.hoverKey == this.SetupKey;
			if (isHover)
				Draw.Rect(box.rect.x, box.rect.y, box.rect.w, box.rect.h, Colors.menuHighlightBox.gain(0.5), BlendMode.Screen);

			Draw.Text(this.Title(), box.rect.x + 10, box.rect.y + 5, DB.thicket, isHover ? Colors.textChoiceHoverActive : Colors.textMain);
			SharedArt.CheckboxBig(g, new Vec(box.rect.w - 10 - 20 - (this.PasteAction is null ? 65 : 125), 1), this.CheckboxKey, this.HasValue(), boxColor: Colors.buttonBoxNormal, noHover: true);

			if (this.PasteAction is { } pasteAction)
				SharedArt.ButtonText(
					g, new Vec(box.rect.w - 10 - 125, -3),
					this.PasteKey,
					ModEntry.Instance.Localizations.Localize(["settings", this.HasValue() ? "clear" : "paste"]),
					onMouseDown: new MouseDownHandler(() =>
					{
						Audio.Play(Event.Click);
						pasteAction(g, this.CurrentRoute, this.HasValue() ? null : (ClipboardService.GetText() ?? ""));
					})
				);

			SharedArt.ButtonText(
				g, new Vec(box.rect.w - 10 - 60, -3),
				this.SetupKey,
				ModEntry.Instance.Localizations.Localize(["settings", "setup"]),
				onMouseDown: new MouseDownHandler(() =>
				{
					Audio.Play(Event.Click);
					this.SetupAction(g, this.CurrentRoute);
				})
			);

			if (box.IsHover() && this.BaseTooltips is { } tooltips)
				g.tooltips.Add(new Vec(box.rect.x2 - Tooltip.WIDTH, box.rect.y2), tooltips());
			if (g.hoverKey == this.PasteKey && (this.PasteTooltips ?? this.BaseTooltips) is { } pasteTooltips)
				g.tooltips.Add(new Vec(box.rect.x2 - Tooltip.WIDTH, box.rect.y2), pasteTooltips());
			if (g.hoverKey == this.SetupKey && (this.SetupTooltips ?? this.BaseTooltips) is { } setupTooltips)
				g.tooltips.Add(new Vec(box.rect.x2 - Tooltip.WIDTH, box.rect.y2), setupTooltips());
		}

		return new(box.rect.w, 20);
	}
}

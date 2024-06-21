using FSPRO;
using Nickel.ModSettings;
using System;
using System.Collections.Generic;
using TextCopy;

namespace Nickel.UpdateChecks;

public sealed class TokenModSetting : ModSetting
{
	public UIKey PasteKey { get; private set; }
	public UIKey SetupKey { get; private set; }
	public UIKey CheckboxKey { get; private set; }
	public required Func<string> Title { get; init; }
	public required Func<bool> HasValue { get; init; }
	public Action<string?>? PasteAction { get; init; }
	public required Action SetupAction { get; init; }
	public Func<IEnumerable<Tooltip>>? BaseTooltips { get; init; }
	public Func<IEnumerable<Tooltip>>? PasteTooltips { get; init; }
	public Func<IEnumerable<Tooltip>>? SetupTooltips { get; init; }

	public override void Initialize(G g, ModSettingsRoute route, Func<UIKey> keyGenerator)
	{
		base.Initialize(g, route, keyGenerator);

		if (this.PasteKey == 0)
			this.PasteKey = keyGenerator();
		if (this.SetupKey == 0)
			this.SetupKey = keyGenerator();
		if (this.CheckboxKey == 0)
			this.CheckboxKey = keyGenerator();
	}

	public override Vec? Render(G g, Box box, bool dontDraw)
	{
		if (!dontDraw)
		{
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
						pasteAction(this.HasValue() ? null : (ClipboardService.GetText() ?? ""));
					})
				);

			SharedArt.ButtonText(
				g, new Vec(box.rect.w - 10 - 60, -3),
				this.SetupKey,
				ModEntry.Instance.Localizations.Localize(["settings", "setup"]),
				onMouseDown: new MouseDownHandler(() =>
				{
					Audio.Play(Event.Click);
					this.SetupAction();
				})
			);

			if (box.IsHover() && this.BaseTooltips is { } tooltips)
				g.tooltips.Add(new Vec(box.rect.x + 10), tooltips());
			if (g.hoverKey == this.PasteKey && (this.PasteTooltips ?? this.BaseTooltips) is { } pasteTooltips)
				g.tooltips.Add(new Vec(box.rect.x + 10), pasteTooltips());
			if (g.hoverKey == this.SetupKey && (this.SetupTooltips ?? this.BaseTooltips) is { } setupTooltips)
				g.tooltips.Add(new Vec(box.rect.x + 10), setupTooltips());
		}

		return new(box.rect.w, 20);
	}
}

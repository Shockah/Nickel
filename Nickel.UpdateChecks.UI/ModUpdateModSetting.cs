using FSPRO;
using Nickel.ModSettings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Nickel.UpdateChecks.UI;

internal sealed class ModUpdateModSetting : IModSettingsApi.IModSetting
{
	public UIKey Key { get; private set; }
	public event IModSettingsApi.OnMenuOpen? OnMenuOpen;
	public event IModSettingsApi.OnMenuClose? OnMenuClose;

	public required IModManifest Mod { get; init; }
	public required SemanticVersion Version { get; init; }
	public required List<UpdateDescriptor> Descriptors { get; init; }
	public required Func<bool> IsIgnored { get; init; }
	public required Action<bool> SetIgnored { get; init; }

	private UIKey IgnoreKey;
	private UK WebsiteKey;

	public ModUpdateModSetting()
	{
		this.OnMenuOpen += (_, _) =>
		{
			if (this.Key == 0)
				this.Key = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
			if (this.IgnoreKey == 0)
				this.IgnoreKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
			if (this.WebsiteKey == 0)
				this.WebsiteKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
		};
	}

	~ModUpdateModSetting()
	{
		if (this.Key != 0)
			ModEntry.Instance.Helper.Utilities.FreeEnumCase(this.Key.k);
		if (this.IgnoreKey != 0)
			ModEntry.Instance.Helper.Utilities.FreeEnumCase(this.IgnoreKey.k);
		if (this.WebsiteKey != 0)
			ModEntry.Instance.Helper.Utilities.FreeEnumCase(this.WebsiteKey);
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

			var isHover = box.IsHover() || g.hoverKey == this.IgnoreKey || g.hoverKey == this.WebsiteKey;
			if (isHover)
				Draw.Rect(box.rect.x, box.rect.y, box.rect.w, box.rect.h, Colors.menuHighlightBox.gain(0.5), BlendMode.Screen);

			Draw.Text($"{ModEntry.Instance.UpdateChecksApi.GetModNameForUpdatePurposes(this.Mod)} <c=textBold>{this.Mod.Version}</c> -> <c=boldPink>{this.Version}</c>", box.rect.x + 10, box.rect.y + 6, DB.pinch, isHover ? Colors.textChoiceHoverActive : Colors.textMain);
			
			var ignoreBox = SharedArt.CheckboxBig(g, new Vec(box.rect.w - 10 - 15, 1), this.IgnoreKey, this.IsIgnored(), boxColor: Colors.buttonBoxNormal, onMouseDown: new MouseDownHandler(() =>
			{
				Audio.Play(Event.Click);
				this.SetIgnored(!this.IsIgnored());
			}));

			if (ignoreBox.isHover)
				g.tooltips.Add(new Vec(box.rect.x2 - Tooltip.WIDTH, box.rect.y2), new GlossaryTooltip($"settings.{ModEntry.Instance.Package.Manifest.UniqueName}::Ignore")
				{
					TitleColor = Colors.textBold,
					Title = ModEntry.Instance.Localizations.Localize(["settings", "ignoreMod", "title"]),
					Description = ModEntry.Instance.Localizations.Localize(["settings", "ignoreMod", "description"])
				});

			var offsetX = box.rect.w - 10 - 15 - 2;

			foreach (var (descriptor, i) in this.Descriptors.Select((url, i) => (url, i)))
			{
				var source = ModEntry.Instance.UpdateChecksApi.LookupUpdateSourceByKey(descriptor.SourceKey);
				var uiSource = source is null ? null : ModEntry.Instance.AsUiUpdateSource(source);
				var offIcon = uiSource?.GetIcon(this.Mod, descriptor, false) ?? ModEntry.Instance.Content.DefaultVisitWebsiteIcon.Sprite;
				var onIcon = uiSource?.GetIcon(this.Mod, descriptor, true) ?? ModEntry.Instance.Content.DefaultVisitWebsiteOnIcon.Sprite;
				var texture = SpriteLoader.Get(offIcon)!;

				offsetX -= texture.Width + 2;
				
				var websiteBox = SharedArt.ButtonSprite(
					g,
					new Rect(offsetX, (18 - texture.Height) / 2, texture.Width, texture.Height),
					new UIKey(this.WebsiteKey, i),
					offIcon,
					onIcon,
					onMouseDown: new MouseDownHandler(() =>
					{
						Audio.Play(Event.Click);
						Process.Start(new ProcessStartInfo(descriptor.Url) { UseShellExecute = true });
					})
				);

				if (websiteBox.isHover)
				{
					var visitWebsiteTooltips = uiSource?.GetVisitWebsiteTooltips(this.Mod, descriptor) ?? [
						new GlossaryTooltip($"settings.{ModEntry.Instance.Package.Manifest.UniqueName}::VisitWebsite")
						{
							TitleColor = Colors.textBold,
							Title = ModEntry.Instance.Localizations.Localize(["settings", "visitWebsite", "title"]),
							Description = ModEntry.Instance.Localizations.Localize(["settings", "visitWebsite", "description"])
						}
					];
					
					g.tooltips.Add(new Vec(box.rect.x2 - Tooltip.WIDTH, box.rect.y2), visitWebsiteTooltips);
				}
			}
		}

		return new(box.rect.w, 18);
	}
}

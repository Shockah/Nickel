using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.Pintail;
using Nanoray.PluginManager;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Nickel.ModSettings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.UpdateChecks.UI;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;
	internal readonly IUpdateChecksApi UpdateChecksApi;

	internal readonly Content Content;

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(
				new JsonLocalizationProvider(
					tokenExtractor: new SimpleLocalizationTokenExtractor(),
					localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
				)
			)
		);
		this.UpdateChecksApi = helper.ModRegistry.GetApi<IUpdateChecksApi>("Nickel.UpdateChecks")!;
		
		this.Content = new()
		{
			UpdateAvailableOverlayIcon = this.Helper.Content.Sprites.RegisterSprite(this.Package.PackageRoot.GetRelativeFile("assets/UpdateAvailableOverlayIcon.png")),
			WarningMessageOverlayIcon = this.Helper.Content.Sprites.RegisterSprite(this.Package.PackageRoot.GetRelativeFile("assets/WarningMessageOverlayIcon.png")),
			ErrorMessageOverlayIcon = this.Helper.Content.Sprites.RegisterSprite(this.Package.PackageRoot.GetRelativeFile("assets/ErrorMessageOverlayIcon.png")),
			UpdateAvailableTooltipIcon = this.Helper.Content.Sprites.RegisterSprite(this.Package.PackageRoot.GetRelativeFile("assets/UpdateAvailableTooltipIcon.png")),
			DefaultVisitWebsiteIcon = this.Helper.Content.Sprites.RegisterSprite(this.Package.PackageRoot.GetRelativeFile("assets/DefaultVisitWebsiteIcon.png")),
			DefaultVisitWebsiteOnIcon = this.Helper.Content.Sprites.RegisterSprite(this.Package.PackageRoot.GetRelativeFile("assets/DefaultVisitWebsiteOnIcon.png")),
		};
		
		helper.ModRegistry.AwaitApi<IModSettingsApi>("Nickel.ModSettings", api =>
		{
			var updateChecksMod = helper.ModRegistry.LoadedMods.GetValueOrDefault("Nickel.UpdateChecks");
			api.OverrideModSettingsTitle(updateChecksMod?.DisplayName ?? updateChecksMod?.UniqueName);
			api.RegisterModSettings(this.MakeUpdateListModSetting());
		});
		
		var harmony = this.Helper.Utilities.Harmony;
		
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CornerMenu), nameof(CornerMenu.Render))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(CornerMenu)}.{nameof(CornerMenu.Render)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CornerMenu_Render_Transpiler))
		);
	}

	public override object GetApi(IModManifest requestingMod)
		=> new ApiImplementation();

	internal IUiUpdateSource? AsUiUpdateSource(IUpdateSource source)
		=> this.Helper.Utilities.ProxyManager.TryProxy<string, IUiUpdateSource>(source, "<unknown>", this.Package.Manifest.UniqueName, out var proxy) ? proxy : null;

	private IEnumerable<(IModManifest Mod, SemanticVersion Version, List<UpdateDescriptor> Descriptors)> GetPendingModUpdates()
		=> this.Helper.ModRegistry.ResolvedMods.Values
			.Select(mod => (Mod: mod, Descriptors: this.UpdateChecksApi.TryGetUpdateInfo(mod, out var descriptors) ? descriptors : null))
			.Where(e => e.Descriptors is not null)
			.Select(e => (Mod: e.Mod, Descriptors: e.Descriptors!))
			.Where(e => e.Descriptors.Count != 0)
			.Select(e => (Mod: e.Mod, Version: e.Descriptors.Max(d => d.Version), Descriptors: e.Descriptors!))
			.Where(e => e.Version > e.Mod.Version)
			.OrderBy(e => this.UpdateChecksApi.GetModNameForUpdatePurposes(e.Mod));

	private IModSettingsApi.IModSetting MakeUpdateListModSetting()
	{
		var api = this.Helper.ModRegistry.GetApi<IModSettingsApi>("Nickel.ModSettings") ?? throw new InvalidOperationException();

		var updateSources = this.GetPendingModUpdates()
			.SelectMany(e => e.Descriptors)
			.GroupBy(e => e.SourceKey)
			.Select(g => (Key: g.Key, Source: this.UpdateChecksApi.LookupUpdateSourceByKey(g.Key), Descriptors: g.ToList()))
			.Where(e => e.Source is not null)
			.Select(e => (Key: e.Key, Source: this.AsUiUpdateSource(e.Source!), Descriptors: e.Descriptors))
			.Where(e => e.Source is not null)
			.Select(e => (Key: e.Key, Source: e.Source!, Name: e.Source!.Name, Descriptors: e.Descriptors))
			.OrderBy(e => e.Name);

		return new DynamicModSetting(() => api.MakeList([
			.. updateSources
				.Select(
					e => api.MakeButton(
						() => this.Localizations.Localize(["settings", "openAll", "title"], new { SourceName = e.Name }),
						(_, _) =>
						{
							foreach (var descriptor in e.Descriptors)
								Process.Start(new ProcessStartInfo(descriptor.Url) { UseShellExecute = true });
						}
					).SetTooltips(() => [
						new GlossaryTooltip($"settings.{this.Package.Manifest.UniqueName}::OpenAll")
						{
							TitleColor = Colors.textBold,
							Title = this.Localizations.Localize(["settings", "openAll", "title"], new { SourceName = e.Name }),
							Description = this.Localizations.Localize(["settings", "openAll", "description"], new { SourceName = e.Name })
						}
					])
				),
			.. this.GetPendingModUpdates()
				.Select(
					e => new ModUpdateModSetting
					{
						Mod = e.Mod,
						Version = e.Version,
						Descriptors = e.Descriptors,
						IsIgnored = () => this.UpdateChecksApi.GetIgnoredUpdateForMod(e.Mod) == e.Version,
						SetIgnored = value => this.UpdateChecksApi.SetIgnoredUpdateForMod(e.Mod, value ? e.Version : null),
					}
				)
		]).SetEmptySetting(
			api.MakeText(() => this.Localizations.Localize(["settings", "upToDate"]))
		).SubscribeToOnMenuClose(
			_ => this.UpdateChecksApi.SaveSettings()
		));
	}

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> CornerMenu_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.LdcI4((int)StableSpr.buttons_menu))
				.Find(ILMatches.Call("Sprite"))
				.Replace(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CornerMenu_Render_Transpiler_HijackDraw))))
				.AllElements();
		}
		catch (Exception ex)
		{
			Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static void CornerMenu_Render_Transpiler_HijackDraw(Spr? id, double x, double y, bool flipX, bool flipY, double rotation, Vec? originPx, Vec? originRel, Vec? scale, Rect? pixelRect, Color? color, BlendState? blend, SamplerState? samplerState, Effect? effect)
	{
		Draw.Sprite(id, x, y, flipX, flipY, rotation, originPx, originRel, scale, pixelRect, color, blend, samplerState, effect);

		var updateSourceMessages = Instance.UpdateChecksApi.UpdateSources
			.OrderBy(kvp => kvp.Key)
			.Select(kvp => kvp.Value)
			.Select(s => Instance.AsUiUpdateSource(s))
			.OfType<IUiUpdateSource>()
			.SelectMany(s => s.Messages)
			.GroupBy(m => m.Level)
			.ToDictionary(g => g.Key, g => g.ToList());

		var updatesAvailable = Instance.GetPendingModUpdates()
			.Where(e => Instance.UpdateChecksApi.GetIgnoredUpdateForMod(e.Mod) != e.Version)
			.ToList();

		List<ISpriteEntry> overlaysToShow = [];
		var addedTooltips = false;

		if (updatesAvailable.Count > 0)
			overlaysToShow.Add(Instance.Content.UpdateAvailableOverlayIcon);

		if (updateSourceMessages.TryGetValue(UpdateSourceMessageLevel.Error, out var messages) && messages.Count > 0)
			overlaysToShow.Add(Instance.Content.ErrorMessageOverlayIcon);
		else if (updateSourceMessages.TryGetValue(UpdateSourceMessageLevel.Warning, out messages) && messages.Count > 0)
			overlaysToShow.Add(Instance.Content.WarningMessageOverlayIcon);

		if (overlaysToShow.Count > 0)
		{
			var overlayToShow = overlaysToShow[(int)MG.inst.g.time % overlaysToShow.Count];
			Draw.Sprite(overlayToShow.Sprite, x, y);
		}

		if (MG.inst.g.boxes.FirstOrDefault(b => b.key is { } key && key.k == StableUK.corner_mainmenu) is not { } box)
			return;
		if (!box.IsHover())
			return;

		if (updateSourceMessages.TryGetValue(UpdateSourceMessageLevel.Error, out messages) && messages.Count > 0)
		{
			if (!addedTooltips)
			{
				addedTooltips = true;
				MG.inst.g.tooltips.Add(box.rect.xy + new Vec(15, 15), new TTDivider());
			}

			var i = 0;
			foreach (var error in messages)
			{
				MG.inst.g.tooltips.Add(box.rect.xy + new Vec(15, 15), new GlossaryTooltip($"ui.{Instance.Package.Manifest.UniqueName}::Error{i++}")
				{
					Icon = StableSpr.icons_hurt,
					TitleColor = Colors.textBold,
					Title = Instance.Localizations.Localize(["settingsTooltip", "error"]),
					Description = error.Message,
				});
			}
		}

		if (updateSourceMessages.TryGetValue(UpdateSourceMessageLevel.Warning, out messages) && messages.Count > 0)
		{
			if (!addedTooltips)
			{
				addedTooltips = true;
				MG.inst.g.tooltips.Add(box.rect.xy + new Vec(15, 15), new TTDivider());
			}

			var i = 0;
			foreach (var error in messages)
			{
				MG.inst.g.tooltips.Add(box.rect.xy + new Vec(15, 15), new GlossaryTooltip($"ui.{Instance.Package.Manifest.UniqueName}::Warning{i++}")
				{
					Icon = StableSpr.icons_hurtBlockable,
					TitleColor = Colors.textBold,
					Title = Instance.Localizations.Localize(["settingsTooltip", "warning"]),
					Description = error.Message,
				});
			}
		}

		if (updateSourceMessages.TryGetValue(UpdateSourceMessageLevel.Info, out messages) && messages.Count > 0)
		{
			if (!addedTooltips)
			{
				addedTooltips = true;
				MG.inst.g.tooltips.Add(box.rect.xy + new Vec(15, 15), new TTDivider());
			}

			var i = 0;
			foreach (var error in messages)
			{
				MG.inst.g.tooltips.Add(box.rect.xy + new Vec(15, 15), new GlossaryTooltip($"ui.{Instance.Package.Manifest.UniqueName}::Info{i++}")
				{
					Icon = StableSpr.icons_hurtBlockable,
					TitleColor = Colors.textBold,
					Title = Instance.Localizations.Localize(["settingsTooltip", "info"]),
					Description = error.Message,
				});
			}
		}

		if (updatesAvailable.Count > 0)
		{
			if (!addedTooltips)
			{
				// addedTooltips = true;
				MG.inst.g.tooltips.Add(box.rect.xy + new Vec(15, 15), new TTDivider());
			}

			MG.inst.g.tooltips.Add(box.rect.xy + new Vec(15, 15), new GlossaryTooltip($"ui.{Instance.Package.Manifest.UniqueName}::UpdatesAvailable")
			{
				Icon = Instance.Content.UpdateAvailableTooltipIcon.Sprite,
				TitleColor = Colors.textBold,
				Title = Instance.Localizations.Localize(["settingsTooltip", "updatesAvailableTooltip"]),
				Description = string.Join("\n", updatesAvailable.Select(e => $"<c=textFaint>{Instance.UpdateChecksApi.GetModNameForUpdatePurposes(e.Mod)}</c> <c=textBold>{e.Mod.Version}</c> -> <c=boldPink>{e.Version}</c>"))
			});
		}
	}
}

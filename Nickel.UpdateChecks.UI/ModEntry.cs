using FSPRO;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Pintail;
using Nanoray.PluginManager;
using Nickel.Common;
using Nickel.InfoScreens;
using Nickel.ModSettings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Nickel.UpdateChecks.UI;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;
	internal readonly IUpdateChecksApi UpdateChecksApi;
	internal readonly IModSettingsApi ModSettingsApi;

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
		this.ModSettingsApi = helper.ModRegistry.GetApi<IModSettingsApi>("Nickel.ModSettings")!;
		
		this.Content = new()
		{
			UpdateAvailableOverlayIcon = this.Helper.Content.Sprites.RegisterSprite(this.Package.PackageRoot.GetRelativeFile("assets/UpdateAvailableOverlayIcon.png")),
			WarningMessageOverlayIcon = this.Helper.Content.Sprites.RegisterSprite(this.Package.PackageRoot.GetRelativeFile("assets/WarningMessageOverlayIcon.png")),
			ErrorMessageOverlayIcon = this.Helper.Content.Sprites.RegisterSprite(this.Package.PackageRoot.GetRelativeFile("assets/ErrorMessageOverlayIcon.png")),
			UpdateAvailableTooltipIcon = this.Helper.Content.Sprites.RegisterSprite(this.Package.PackageRoot.GetRelativeFile("assets/UpdateAvailableTooltipIcon.png")),
			DefaultVisitWebsiteIcon = this.Helper.Content.Sprites.RegisterSprite(this.Package.PackageRoot.GetRelativeFile("assets/DefaultVisitWebsiteIcon.png")),
			DefaultVisitWebsiteOnIcon = this.Helper.Content.Sprites.RegisterSprite(this.Package.PackageRoot.GetRelativeFile("assets/DefaultVisitWebsiteOnIcon.png")),
		};
		
		var updateChecksMod = helper.ModRegistry.LoadedMods.GetValueOrDefault("Nickel.UpdateChecks");
		this.ModSettingsApi.OverrideModSettingsTitle(updateChecksMod?.DisplayName ?? updateChecksMod?.UniqueName);
		this.ModSettingsApi.RegisterModSettings(this.MakeUpdateListModSetting());

		if (helper.ModRegistry.GetApi<IInfoScreensApi>("Nickel.InfoScreens") is { } infoScreensApi)
		{
			helper.Events.OnModLoadPhaseFinished += (_, phase) =>
			{
				if (phase != ModLoadPhase.AfterDbInit)
					return;
				
				this.UpdateChecksApi.AwaitAllUpdateInfo(updates =>
				{
					var pendingUpdates = this.GetPendingModUpdates().ToList();
					if (pendingUpdates.Count == 0)
						return;
				
					var route = infoScreensApi.CreateBasicInfoScreenRoute();
					route.Paragraphs = [
						infoScreensApi.CreateBasicInfoScreenParagraph(this.Localizations.Localize(["infoScreen", "title"])).SetFont(DB.thicket),
						infoScreensApi.CreateBasicInfoScreenParagraph(
							pendingUpdates.Join(e => $"<c=textFaint>{this.UpdateChecksApi.GetModNameForUpdatePurposes(e.Mod)}</c> <c=textBold>{e.Mod.Version}</c> -> <c=boldPink>{e.Version}</c>", "\n")
						),
					];
					route.Actions = [
						infoScreensApi.CreateBasicInfoScreenAction(this.Localizations.Localize(["infoScreen", "actions", "details"]), args =>
						{
							Audio.Play(Event.Click);
							args.Route.RouteOverride = this.ModSettingsApi.MakeModSettingsRouteForMod(package.Manifest);
						}),
						infoScreensApi.CreateBasicInfoScreenAction(this.Localizations.Localize(["infoScreen", "actions", "remindLater"]), args =>
						{
							Audio.Play(Event.Click);
							args.G.CloseRoute(args.Route.AsRoute);
						}).SetControllerKeybind(Btn.B),
						infoScreensApi.CreateBasicInfoScreenAction(this.Localizations.Localize(["infoScreen", "actions", "ignore"]), args =>
						{
							Audio.Play(Event.Click);
							
							foreach (var kvp in updates)
								if (kvp.Value is not null && kvp.Value.Count != 0)
									this.UpdateChecksApi.SetIgnoredUpdateForMod(kvp.Key, kvp.Value.Select(u => u.Version).Max());
							this.UpdateChecksApi.SaveSettings();
							
							args.G.CloseRoute(args.Route.AsRoute);
						}).SetColor(Colors.redd).SetRequiresConfirmation(true),
					];

					infoScreensApi.RequestInfoScreen("UpdatesAvailable", route.AsRoute, 1_000);
				});
			};
		}
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
}

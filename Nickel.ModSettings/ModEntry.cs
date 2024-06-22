using daisyowl.text;
using FSPRO;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nickel.ModSettings;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	private readonly Dictionary<string, IModSettingsApi.IModSetting> ModSettings = [];

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

		var harmony = new Harmony(package.Manifest.UniqueName);

		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(MainMenu), nameof(MainMenu.Render))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(MainMenu)}.{nameof(MainMenu.Render)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(MainMenu_Render_Postfix))
		);
	}

	public override object? GetApi(IModManifest requestingMod)
		=> new ApiImplementation(requestingMod);

	internal void RegisterModSettings(IModManifest modManifest, IModSettingsApi.IModSetting settings)
		=> this.ModSettings[modManifest.UniqueName] = settings;

	private static void MainMenu_Render_Postfix(MainMenu __instance, G g)
	{
		if (__instance.subRoute is not null)
			return;

		SharedArt.ButtonText(
			g, new Vec(405.0, 42.0),
			(UK)500_000,
			Instance.Localizations.Localize(["mainMenu", "buttonTitle"]),
			onMouseDown: new MouseDownHandler(() =>
			{
				Audio.Play(Event.Click);
				__instance.subRoute = new ModSettingsRoute
				{
					Setting = new ListModSetting
					{
						Spacing = 8,
						Settings = [
							new PaddingModSetting
							{
								Setting = new TextModSetting
								{
									Text = () => Instance.Localizations.Localize(["modSettings", "title"]),
									Font = DB.stapler,
									Alignment = TAlign.Center,
									WrapText = false,
								},
								TopPadding = 4,
								BottomPadding = 4,
							},
							new ListModSetting
							{
								Settings = [
									.. Instance.Helper.ModRegistry.LoadedMods.Values
										.OrderBy(m => m.DisplayName ?? m.UniqueName)
										.Select(m => (Mod: m, Setting: Instance.ModSettings.TryGetValue(m.UniqueName, out var setting) ? setting : null))
										.Where(e => e.Setting is not null)
										.Select(e => new ButtonModSetting
										{
											Title = () => e.Mod.DisplayName ?? e.Mod.UniqueName,
											OnClick = (g, route) => route.OpenSubroute(g, new ModSettingsRoute
											{
												Setting = new ListModSetting
												{
													Spacing = 8,
													Settings = [
														new PaddingModSetting
														{
															Setting = new TextModSetting
															{
																Text = () => e.Mod.DisplayName ?? e.Mod.UniqueName,
																Font = DB.stapler,
																Alignment = TAlign.Center,
																WrapText = false,
															},
															TopPadding = 4,
															BottomPadding = 4,
														},
														e.Setting!,
														new ButtonModSetting
														{
															Title = () => Instance.Localizations.Localize(["modSettings", "back"]),
															OnClick = (g, route) => route.CloseRoute(g)
														},
													]
												}
											})
										})
								],
								EmptySetting = new TextModSetting
								{
									Text = () => Instance.Localizations.Localize(["modSettings", "noMods"])
								},
							},
							new ButtonModSetting
							{
								Title = () => Instance.Localizations.Localize(["modSettings", "back"]),
								OnClick = (g, route) => route.CloseRoute(g)
							},
						],
					},
				};
			})
		);
	}
}

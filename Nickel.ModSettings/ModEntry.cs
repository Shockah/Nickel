using FSPRO;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nickel.ModSettings;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;

	internal readonly Dictionary<string, string> ModTitleOverrides = [];
	internal readonly Dictionary<string, IModSettingsApi.IModSetting> ModSettings = [];
	private readonly ApiImplementation Api;
	
	private static UK ModSettingsButtonKey;

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		ModSettingsButtonKey = Instance.Helper.Utilities.ObtainEnumCase<UK>();
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(
				new JsonLocalizationProvider(
					tokenExtractor: new SimpleLocalizationTokenExtractor(),
					localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
				)
			)
		);
		this.Api = new(this.Helper.ModRegistry.ModLoaderModManifest);

		this.Api.RegisterModSettings(this.Api.MakeList([
			this.Api.MakeCheckbox(
				() => this.Localizations.Localize(["ownSettings", "debug", "title"]),
				() => Nickel.Instance.RunInfo.Settings.DebugMode != DebugMode.Disabled,
				setter: (_, _, value) =>
				{
					Nickel.Instance.RunInfo.Settings.DebugMode = value ? DebugMode.EnabledWithSaving : DebugMode.Disabled;
					OnSettingsUpdate();
				}
			),
			this.Api.MakeConditional(
				this.Api.MakeCheckbox(
					() => this.Localizations.Localize(["ownSettings", "debugAutoSave", "title"]),
					() => Nickel.Instance.RunInfo.Settings.DebugMode == DebugMode.EnabledWithSaving,
					setter: (_, _, value) =>
					{
						Nickel.Instance.RunInfo.Settings.DebugMode = value ? DebugMode.EnabledWithSaving : DebugMode.Enabled;
						OnSettingsUpdate();
					}
				),
				() => Nickel.Instance.RunInfo.Settings.DebugMode != DebugMode.Disabled
			),
			this.Api.MakeConditional(
				setting: this.Api.MakeButton(
					title: () => this.Localizations.Localize(["ownSettings", "toggleDebugMenu", "title"]),
					(g, _) =>
					{
						Audio.Play(Event.Click);
						if (g.e is { } editor)
							editor.isActive = !editor.isActive;
					}
				),
				isVisible: () => Nickel.Instance.RunInfo.Settings.DebugMode != DebugMode.Disabled
			)
		]).SubscribeToOnMenuClose(_ =>
		{
			var modLoaderHelper = helper.ModRegistry.GetModHelper(this.Helper.ModRegistry.ModLoaderModManifest);
			modLoaderHelper.Storage.SaveJson(modLoaderHelper.Storage.GetMainStorageFile("json"), Nickel.Instance.RunInfo.Settings);
			OnSettingsUpdate();
		}));

		var harmony = helper.Utilities.Harmony;

		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(MainMenu), nameof(MainMenu.Render))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(MainMenu)}.{nameof(MainMenu.Render)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(MainMenu_Render_Postfix))
		);
	}

	public override object GetApi(IModManifest requestingMod)
		=> new ApiImplementation(requestingMod);

	private static void OnSettingsUpdate()
	{
		FeatureFlags.Debug = Nickel.Instance.RunInfo.Settings.DebugMode != DebugMode.Disabled;

		// ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
		if (MG.inst?.g is not { } g)
			return;

		if (FeatureFlags.Debug && g.e is null)
		{
			g.e = new Editor();
			g.e.IMGUI_Setup(MG.inst);
		}
	}

	private static void MainMenu_Render_Postfix(MainMenu __instance, G g)
	{
		if (__instance.subRoute is not null)
			return;

		SharedArt.ButtonText(
			g, new Vec(405.0, 42.0),
			ModSettingsButtonKey,
			Instance.Localizations.Localize(["mainMenu", "buttonTitle"]),
			onMouseDown: new MouseDownHandler(() =>
			{
				Audio.Play(Event.Click);
				__instance.subRoute = Instance.Api.MakeModSettingsRouteForAllMods();
			})
		);
	}
}

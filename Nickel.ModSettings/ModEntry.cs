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
	internal readonly ApiImplementation Api;
	
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
		this.Api = new(package.Manifest);

		var harmony = helper.Utilities.Harmony;

		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(MainMenu), nameof(MainMenu.Render))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(MainMenu)}.{nameof(MainMenu.Render)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(MainMenu_Render_Postfix))
		);
	}

	public override object? GetApi(IModManifest requestingMod)
		=> new ApiImplementation(requestingMod);

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

using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using System.Collections.Generic;

namespace Nickel.Essentials;

public sealed class ModEntry : SimpleMod
{
	internal static ModEntry Instance { get; private set; } = null!;
	internal readonly ILocaleBoundNonNullLocalizationProvider<IReadOnlyList<string>> Localizations;
	internal readonly ApiImplementation Api;
	internal readonly Settings Settings;

	// internal static bool StopStateTransitions;

	internal readonly ISpriteEntry ScrollUpSprite;
	internal readonly ISpriteEntry ScrollUpOnSprite;
	internal readonly ISpriteEntry ScrollDownSprite;
	internal readonly ISpriteEntry ScrollDownOnSprite;

	internal readonly HookManager<IEssentialsApi.IHook> Hooks;

	private IWritableFileInfo SettingsFile
		=> this.Helper.Storage.GetMainStorageFile("json");

	public ModEntry(IPluginPackage<IModManifest> package, IModHelper helper, ILogger logger) : base(package, helper, logger)
	{
		Instance = this;
		this.Hooks = new(package.Manifest.UniqueName);
		this.Api = new();
		this.Localizations = new MissingPlaceholderLocalizationProvider<IReadOnlyList<string>>(
			new CurrentLocaleOrEnglishLocalizationProvider<IReadOnlyList<string>>(
				new JsonLocalizationProvider(
					tokenExtractor: new SimpleLocalizationTokenExtractor(),
					localeStreamFunction: locale => package.PackageRoot.GetRelativeFile($"i18n/{locale}.json").OpenRead()
				)
			)
		);
		this.Settings = helper.Storage.LoadJson<Settings>(this.SettingsFile);
		
		this.ScrollUpSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/ScrollUp.png"));
		this.ScrollUpOnSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/ScrollUpOn.png"));
		this.ScrollDownSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/ScrollDown.png"));
		this.ScrollDownOnSprite = helper.Content.Sprites.RegisterSprite(package.PackageRoot.GetRelativeFile("assets/ScrollDownOn.png"));
		
		helper.ModRegistry.AwaitApi<IModSettingsApi>(
			"Nickel.ModSettings",
			api => api.RegisterModSettings(
				api.MakeList([
					api.MakeProfileSelector(
						() => package.Manifest.DisplayName ?? package.Manifest.UniqueName,
						this.Settings.ProfileBased
					),
					CrewAndShipSorting.MakeSettings(api),
					CrewSelection.MakeSettings(api),
					// StarterDeckPreview.MakeSettings(api),
					ExeBlacklist.MakeSettings(api),
					ModDescriptions.MakeSettings(api),
				]).SubscribeToOnMenuClose(
					_ =>
					{
						helper.Storage.SaveJson(this.SettingsFile, this.Settings);
						CrewAndShipSorting.Refresh();
					}
				)
			)
		);

		var harmony = helper.Utilities.Harmony;
		CardCodexFiltering.ApplyPatches(harmony);
		CrewAndShipSorting.ApplyPatches(harmony);
		CrewSelection.ApplyPatches(harmony);
		DebugMenuImprovements.ApplyPatches(harmony);
		ExeBlacklist.ApplyPatches(harmony);
		LogbookReplacement.ApplyPatches(harmony);
		MemorySelection.ApplyPatches(harmony);
		ModDescriptions.ApplyPatches(harmony);
		SaveImport.ApplyPatches(harmony);
		ShipSelection.ApplyPatches(harmony);
		// StarterDeckPreview.ApplyPatches(harmony);
		TooltipScrolling.ApplyPatches(harmony);
		UnlockedWindowResize.ApplyPatches(harmony);

		// harmony.Patch(
		// 	original: AccessTools.DeclaredMethod(typeof(State), nameof(State.ShuffleDeck))
		// 		?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(State)}.{nameof(State.ShuffleDeck)}`"),
		// 	prefix: new HarmonyMethod(this.GetType(), nameof(State_ShuffleDeck_Prefix))
		// );
		// harmony.Patch(
		// 	original: AccessTools.DeclaredMethod(typeof(State), nameof(State.GoToZone))
		// 		?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(State)}.{nameof(State.GoToZone)}`"),
		// 	prefix: new HarmonyMethod(this.GetType(), nameof(State_GoToZone_Prefix))
		// );
	}

	public override object GetApi(IModManifest requestingMod)
		=> new ApiImplementation();

	// private static bool State_ShuffleDeck_Prefix()
	// 	=> !StopStateTransitions;
	//
	// private static bool State_GoToZone_Prefix(ref Route __result)
	// {
	// 	if (!StopStateTransitions)
	// 		return true;
	//
	// 	__result = new Route();
	// 	return false;
	// }
}

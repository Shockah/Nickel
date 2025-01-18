using HarmonyLib;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Nickel.Essentials;

internal sealed partial class ProfileSettings
{
	[JsonProperty]
	public bool SortModdedCrewByName;
	
	[JsonProperty]
	public bool SortVanillaCrewByName;
	
	[JsonProperty]
	public bool SortModdedShipsByName;
	
	[JsonProperty]
	public bool SortVanillaShipsByName;
}

internal static class CrewAndShipSorting
{
	private static List<Deck>? OriginalCharacterOrder;
	private static List<KeyValuePair<string, StarterShip>>? OriginalShipOrder;
	private static List<Deck>? SortedCharacterOrder;
	private static List<KeyValuePair<string, StarterShip>>? SortedShipOrder;

	public static void ApplyPatches(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.OnEnter)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_OnEnter_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.Render)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_Render_Prefix_First)), priority: Priority.First),
			finalizer: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_Render_Finalizer_Last)), priority: Priority.Last)
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.Randomize)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_Randomize_Prefix_First)), priority: Priority.First),
			finalizer: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_Randomize_Finalizer_Last)), priority: Priority.Last)
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(RunConfig), nameof(RunConfig.CycleShip)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RunConfig_CycleShip_Prefix_First)), priority: Priority.First),
			finalizer: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RunConfig_CycleShip_Finalizer_Last)), priority: Priority.Last)
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(RunConfig), nameof(RunConfig.SetShipIdx)),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RunConfig_SetShipIdx_Prefix_First)), priority: Priority.First),
			finalizer: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RunConfig_SetShipIdx_Finalizer_Last)), priority: Priority.Last)
		);
	}

	public static IModSettingsApi.IModSetting MakeSettings(IModSettingsApi api)
		=> api.MakeList([
			api.MakeCheckbox(
				title: () => ModEntry.Instance.Localizations.Localize(["crewAndShipSorting", "sortCrewByName", "name"]),
				getter: () => ModEntry.Instance.Settings.ProfileBased.Current.SortModdedCrewByName,
				setter: (_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.SortModdedCrewByName = value
			).SetTooltips(() => [
				new GlossaryTooltip($"settings.{ModEntry.Instance.Package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}::CrewAndShipSorting::{nameof(ProfileSettings.SortModdedCrewByName)}")
				{
					TitleColor = Colors.textBold,
					Title = ModEntry.Instance.Localizations.Localize(["crewAndShipSorting", "sortCrewByName", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["crewAndShipSorting", "sortCrewByName", "description"])
				}
			]),
			api.MakeConditional(
				api.MakeCheckbox(
					title: () => ModEntry.Instance.Localizations.Localize(["crewAndShipSorting", "sortVanillaCrewByName", "name"]),
					getter: () => ModEntry.Instance.Settings.ProfileBased.Current.SortVanillaCrewByName,
					setter: (_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.SortVanillaCrewByName = value
				).SetTooltips(() => [
					new GlossaryTooltip($"settings.{ModEntry.Instance.Package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}::CrewAndShipSorting::{nameof(ProfileSettings.SortVanillaCrewByName)}")
					{
						TitleColor = Colors.textBold,
						Title = ModEntry.Instance.Localizations.Localize(["crewAndShipSorting", "sortVanillaCrewByName", "name"]).Trim(),
						Description = ModEntry.Instance.Localizations.Localize(["crewAndShipSorting", "sortVanillaCrewByName", "description"])
					}
				]),
				() => ModEntry.Instance.Settings.ProfileBased.Current.SortModdedCrewByName
			),
			api.MakeCheckbox(
				title: () => ModEntry.Instance.Localizations.Localize(["crewAndShipSorting", "sortShipsByName", "name"]),
				getter: () => ModEntry.Instance.Settings.ProfileBased.Current.SortModdedShipsByName,
				setter: (_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.SortModdedShipsByName = value
			).SetTooltips(() => [
				new GlossaryTooltip($"settings.{ModEntry.Instance.Package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}::CrewAndShipSorting::{nameof(ProfileSettings.SortModdedShipsByName)}")
				{
					TitleColor = Colors.textBold,
					Title = ModEntry.Instance.Localizations.Localize(["crewAndShipSorting", "sortShipsByName", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["crewAndShipSorting", "sortShipsByName", "description"])
				}
			]),
			api.MakeConditional(
				api.MakeCheckbox(
					title: () => ModEntry.Instance.Localizations.Localize(["crewAndShipSorting", "sortVanillaShipsByName", "name"]),
					getter: () => ModEntry.Instance.Settings.ProfileBased.Current.SortVanillaShipsByName,
					setter: (_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.SortVanillaShipsByName = value
				).SetTooltips(() => [
					new GlossaryTooltip($"settings.{ModEntry.Instance.Package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}::CrewAndShipSorting::{nameof(ProfileSettings.SortVanillaShipsByName)}")
					{
						TitleColor = Colors.textBold,
						Title = ModEntry.Instance.Localizations.Localize(["crewAndShipSorting", "sortVanillaShipsByName", "name"]).Trim(),
						Description = ModEntry.Instance.Localizations.Localize(["crewAndShipSorting", "sortVanillaShipsByName", "description"])
					}
				]),
				() => ModEntry.Instance.Settings.ProfileBased.Current.SortModdedShipsByName
			)
		]);

	[MemberNotNull(nameof(OriginalCharacterOrder), nameof(OriginalShipOrder), nameof(SortedCharacterOrder), nameof(SortedShipOrder))]
	internal static void Refresh()
	{
		OriginalCharacterOrder ??= NewRunOptions.allChars.ToList();
		OriginalShipOrder ??= StarterShip.ships.ToList();
		
		if (ModEntry.Instance.Settings.ProfileBased.Current.SortModdedCrewByName)
		{
			if (ModEntry.Instance.Settings.ProfileBased.Current.SortVanillaCrewByName)
			{
				SortedCharacterOrder = OriginalCharacterOrder
					.Select(deck => ModEntry.Instance.Helper.Content.Decks.LookupByDeck(deck))
					.OfType<IDeckEntry>()
					.OrderBy(e => e.Configuration.Name?.Invoke(DB.currentLocale.locale))
					.ThenBy(e => e.Deck)
					.Select(e => e.Deck)
					.ToList();
			}
			else
			{
				SortedCharacterOrder = [
					.. OriginalCharacterOrder
						.Select(deck => ModEntry.Instance.Helper.Content.Decks.LookupByDeck(deck))
						.OfType<IDeckEntry>()
						.Where(e => e.ModOwner == ModEntry.Instance.Helper.ModRegistry.VanillaModManifest)
						.OrderBy(e => e.Deck == Deck.colorless ? int.MaxValue : (int)e.Deck)
						.Select(e => e.Deck),
					.. OriginalCharacterOrder
						.Select(deck => ModEntry.Instance.Helper.Content.Decks.LookupByDeck(deck))
						.OfType<IDeckEntry>()
						.Where(e => e.ModOwner != ModEntry.Instance.Helper.ModRegistry.VanillaModManifest)
						.OrderBy(e => e.Configuration.Name?.Invoke(DB.currentLocale.locale))
						.ThenBy(e => e.Deck)
						.Select(e => e.Deck)
				];
			}
		}
		else
		{
			SortedCharacterOrder = OriginalCharacterOrder;
		}
		
		if (ModEntry.Instance.Settings.ProfileBased.Current.SortModdedShipsByName)
		{
			if (ModEntry.Instance.Settings.ProfileBased.Current.SortVanillaShipsByName)
			{
				SortedShipOrder = OriginalShipOrder
					.Select(kvp => ModEntry.Instance.Helper.Content.Ships.LookupByUniqueName(kvp.Key))
					.OfType<IShipEntry>()
					.OrderBy(e => e.Configuration.Name?.Invoke(DB.currentLocale.locale))
					.ThenBy(e => OriginalShipOrder.FindIndex(kvp => kvp.Key == e.UniqueName))
					.Select(e => new KeyValuePair<string, StarterShip>(e.UniqueName, e.Configuration.Ship))
					.ToList();
			}
			else
			{
				SortedShipOrder = [
					.. OriginalShipOrder
						.Select(kvp => ModEntry.Instance.Helper.Content.Ships.LookupByUniqueName(kvp.Key))
						.OfType<IShipEntry>()
						.Where(e => e.ModOwner == ModEntry.Instance.Helper.ModRegistry.VanillaModManifest)
						.OrderBy(e => OriginalShipOrder.FindIndex(kvp => kvp.Key == e.UniqueName))
						.Select(e => new KeyValuePair<string, StarterShip>(e.UniqueName, e.Configuration.Ship)),
					.. OriginalShipOrder
						.Select(kvp => ModEntry.Instance.Helper.Content.Ships.LookupByUniqueName(kvp.Key))
						.OfType<IShipEntry>()
						.Where(e => e.ModOwner != ModEntry.Instance.Helper.ModRegistry.VanillaModManifest)
						.OrderBy(e => e.Configuration.Name?.Invoke(DB.currentLocale.locale))
						.ThenBy(e => OriginalShipOrder.FindIndex(kvp => kvp.Key == e.UniqueName))
						.Select(e => new KeyValuePair<string, StarterShip>(e.UniqueName, e.Configuration.Ship))
				];
			}
		}
		else
		{
			SortedShipOrder = OriginalShipOrder;
		}
	}

	private static bool SetToSorted()
	{
		if (SortedCharacterOrder is null || SortedShipOrder is null)
			Refresh();

		var didSomething = false;

		if (!NewRunOptions.allChars.SequenceEqual(SortedCharacterOrder))
		{
			didSomething = true;
			NewRunOptions.allChars.Clear();
			NewRunOptions.allChars.AddRange(SortedCharacterOrder);
		}

		if (!StarterShip.ships.Keys.SequenceEqual(SortedShipOrder.Select(k => k.Key)))
		{
			didSomething = true;
			StarterShip.ships.Clear();
			foreach (var (key, value) in SortedShipOrder)
				StarterShip.ships[key] = value;
		}

		return didSomething;
	}

	// ReSharper disable once UnusedMethodReturnValue.Local
	private static bool SetToOriginal()
	{
		if (OriginalCharacterOrder is null || OriginalShipOrder is null)
			return false;

		var didSomething = false;

		if (!NewRunOptions.allChars.SequenceEqual(OriginalCharacterOrder))
		{
			didSomething = true;
			NewRunOptions.allChars.Clear();
			NewRunOptions.allChars.AddRange(OriginalCharacterOrder);
		}

		if (!StarterShip.ships.Keys.SequenceEqual(OriginalShipOrder.Select(k => k.Key)))
		{
			didSomething = true;
			StarterShip.ships.Clear();
			foreach (var (key, value) in OriginalShipOrder)
				StarterShip.ships[key] = value;
		}

		return didSomething;
	}

	private static void NewRunOptions_OnEnter_Postfix()
		=> Refresh();

	private static void NewRunOptions_Render_Prefix_First(NewRunOptions __instance, out bool __state)
		=> __state = __instance.subRoute is null && SetToSorted();

	private static void NewRunOptions_Render_Finalizer_Last(in bool __state)
	{
		if (__state)
			SetToOriginal();
	}

	private static void NewRunOptions_Randomize_Prefix_First(out bool __state)
		=> __state = SetToSorted();

	private static void NewRunOptions_Randomize_Finalizer_Last(in bool __state)
	{
		if (__state)
			SetToOriginal();
	}

	private static void RunConfig_CycleShip_Prefix_First(out bool __state)
		=> __state = SetToSorted();

	private static void RunConfig_CycleShip_Finalizer_Last(in bool __state)
	{
		if (__state)
			SetToOriginal();
	}

	private static void RunConfig_SetShipIdx_Prefix_First(out bool __state)
		=> __state = SetToSorted();

	private static void RunConfig_SetShipIdx_Finalizer_Last(in bool __state)
	{
		if (__state)
			SetToOriginal();
	}
}

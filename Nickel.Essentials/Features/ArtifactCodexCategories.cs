using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Essentials;

internal sealed partial class ProfileSettings
{
	[JsonProperty]
	public bool ShipArtifactCodexCategory = true;
	
	[JsonProperty]
	public bool EventArtifactCodexCategory = true;
}

internal static class ArtifactCodexCategories
{
	private static ArtifactBrowse? LastRoute;
	private static bool RenderingArtifactBrowse;
	private static readonly List<string> ShipArtifactKeys = [];
	private static readonly List<string> EventArtifactKeys = [];

	public static void ApplyPatches(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ArtifactBrowse), nameof(ArtifactBrowse.Render))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(ArtifactBrowse)}.{nameof(ArtifactBrowse.Render)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactBrowse_Render_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactBrowse_Render_Finalizer)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactBrowse_Render_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Character), nameof(Character.GetDisplayName), [typeof(string), typeof(State)])
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Character)}.{nameof(Character.GetDisplayName)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Character_GetDisplayName_Postfix))
		);
	}

	public static IModSettingsApi.IModSetting MakeSettings(IModSettingsApi api)
		=> api.MakeList([
			api.MakeCheckbox(
				title: () => ModEntry.Instance.Localizations.Localize(["artifactCodexCategories", "shipCategory", "setting", "name"]),
				getter: () => ModEntry.Instance.Settings.ProfileBased.Current.ShipArtifactCodexCategory,
				setter: (_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.ShipArtifactCodexCategory = value
			).SetTooltips(() => [
				new GlossaryTooltip($"settings.{ModEntry.Instance.Package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}::{nameof(ProfileSettings.ShipArtifactCodexCategory)}")
				{
					TitleColor = Colors.textBold,
					Title = ModEntry.Instance.Localizations.Localize(["artifactCodexCategories", "shipCategory", "setting", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["artifactCodexCategories", "shipCategory", "setting", "description"])
				}
			]),
			api.MakeCheckbox(
				title: () => ModEntry.Instance.Localizations.Localize(["artifactCodexCategories", "eventCategory", "setting", "name"]),
				getter: () => ModEntry.Instance.Settings.ProfileBased.Current.EventArtifactCodexCategory,
				setter: (_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.EventArtifactCodexCategory = value
			).SetTooltips(() => [
				new GlossaryTooltip($"settings.{ModEntry.Instance.Package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}::{nameof(ProfileSettings.EventArtifactCodexCategory)}")
				{
					TitleColor = Colors.textBold,
					Title = ModEntry.Instance.Localizations.Localize(["artifactCodexCategories", "eventCategory", "setting", "name"]),
					Description = ModEntry.Instance.Localizations.Localize(["artifactCodexCategories", "eventCategory", "setting", "description"])
				}
			])
		]);

	private static void ArtifactBrowse_Render_Prefix()
		=> RenderingArtifactBrowse = true;

	private static void ArtifactBrowse_Render_Finalizer()
		=> RenderingArtifactBrowse = false;

	private static IEnumerable<CodeInstruction> ArtifactBrowse_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Stloc<List<(Deck, List<KeyValuePair<string, Type>>)>>(originalMethod))
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ArtifactBrowse_Render_Transpiler_ModifyArtifacts)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static List<(Deck, List<KeyValuePair<string, Type>>)> ArtifactBrowse_Render_Transpiler_ModifyArtifacts(List<(Deck, List<KeyValuePair<string, Type>>)> allArtifacts, ArtifactBrowse route)
	{
		var colorlessIndex = allArtifacts.FindIndex(e => e.Item1 == Deck.colorless);
		if (colorlessIndex == -1)
			return allArtifacts;
		
		if (route != LastRoute)
		{
			LastRoute = route;

			var starterShipArtifacts = StarterShip.ships.Values
				.SelectMany(ship => ship.artifacts)
				.Select(a => a.GetType())
				.ToHashSet();
			var exclusiveShipArtifacts = StarterShip.ships.Keys
				.SelectMany(shipKey => ModEntry.Instance.Helper.Content.Ships.LookupByUniqueName(shipKey)?.Configuration.ExclusiveArtifactTypes ?? new HashSet<Type>())
				.ToHashSet();
			
			ShipArtifactKeys.Clear();
			ShipArtifactKeys.AddRange(
				allArtifacts[colorlessIndex].Item2
					.Where(kvp => starterShipArtifacts.Contains(kvp.Value) || exclusiveShipArtifacts.Contains(kvp.Value))
					.Select(kvp => kvp.Key)
			);

			EventArtifactKeys.Clear();
			EventArtifactKeys.AddRange(
				allArtifacts[colorlessIndex].Item2
					.Where(kvp => DB.artifactMetas[kvp.Key].pools.Contains(ArtifactPool.EventOnly))
					.Where(kvp => !ShipArtifactKeys.Contains(kvp.Key))
					.Where(kvp => kvp.Value != typeof(HARDMODE))
					.Select(kvp => kvp.Key)
			);
		}
		
		if (ModEntry.Instance.Settings.ProfileBased.Current.EventArtifactCodexCategory)
		{
			allArtifacts[colorlessIndex].Item2.RemoveAll(kvp => EventArtifactKeys.Contains(kvp.Key));
			allArtifacts.Insert(colorlessIndex + 1, (Deck.ephemeral, EventArtifactKeys.ToDictionary(k => k, k => DB.artifacts[k]).ToList()));
		}
		
		if (ModEntry.Instance.Settings.ProfileBased.Current.ShipArtifactCodexCategory)
		{
			allArtifacts[colorlessIndex].Item2.RemoveAll(kvp => ShipArtifactKeys.Contains(kvp.Key));
			allArtifacts.Insert(colorlessIndex + 1, (Deck.ares, ShipArtifactKeys.ToDictionary(k => k, k => DB.artifacts[k]).ToList()));
		}

		return allArtifacts;
	}

	private static void Character_GetDisplayName_Postfix(string charId, ref string __result)
	{
		if (!RenderingArtifactBrowse)
			return;

		switch (charId)
		{
			case nameof(Deck.ares):
				if (ModEntry.Instance.Settings.ProfileBased.Current.ShipArtifactCodexCategory)
					__result = ModEntry.Instance.Localizations.Localize(["artifactCodexCategories", "shipCategory", "title"]);
				break;
			case nameof(Deck.ephemeral):
				if (ModEntry.Instance.Settings.ProfileBased.Current.EventArtifactCodexCategory)
					__result = ModEntry.Instance.Localizations.Localize(["artifactCodexCategories", "eventCategory", "title"]);
				break;
		}
	}
}

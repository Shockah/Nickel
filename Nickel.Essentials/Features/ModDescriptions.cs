using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Essentials;

internal static class ModDescriptions
{
	public static void ApplyPatches(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Artifact), nameof(Artifact.GetTooltips)),
			postfix: new HarmonyMethod(AccessTools.Method(typeof(ModDescriptions), nameof(Artifact_GetTooltips_Postfix)), priority: Priority.Last)
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetAllTooltips)),
			postfix: new HarmonyMethod(AccessTools.Method(typeof(ModDescriptions), nameof(Card_GetAllTooltips_Postfix)), priority: Priority.Last)
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(StatusMeta), nameof(StatusMeta.GetTooltips)),
			postfix: new HarmonyMethod(AccessTools.Method(typeof(ModDescriptions), nameof(StatusMeta_GetTooltips_Postfix)), priority: Priority.Last)
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.Render)),
			transpiler: new HarmonyMethod(AccessTools.Method(typeof(ModDescriptions), nameof(NewRunOptions_Render_Transpiler)))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Character), nameof(Character.Render)),
			transpiler: new HarmonyMethod(AccessTools.Method(typeof(ModDescriptions), nameof(Character_Render_Transpiler)))
		);
	}

	private static List<Tooltip> AddModTooltipIfNeeded(List<Tooltip> tooltips, IModManifest? manifest)
	{
		if (!Input.alt)
			return tooltips;
		if (manifest is null)
			return tooltips;

		var modName = string.IsNullOrEmpty(manifest.DisplayName) ? manifest.UniqueName : manifest.DisplayName;
		var text = ModEntry.Instance.Localizations.Localize(["modDescription"], new { ModName = modName });
		return tooltips
			.Where(t => t is not CustomTTText)
			.Prepend(new CustomTTText(text))
			.ToList();
	}

	private static void Artifact_GetTooltips_Postfix(Artifact __instance, ref List<Tooltip> __result)
		=> __result = AddModTooltipIfNeeded(__result, ModEntry.Instance.Helper.Content.Artifacts.LookupByArtifactType(__instance.GetType())?.ModOwner);

	private static void Card_GetAllTooltips_Postfix(Card __instance, ref IEnumerable<Tooltip> __result)
		=> __result = AddModTooltipIfNeeded(__result.ToList(), ModEntry.Instance.Helper.Content.Cards.LookupByCardType(__instance.GetType())?.ModOwner);

	private static void StatusMeta_GetTooltips_Postfix(Status status, ref List<Tooltip> __result)
		=> __result = AddModTooltipIfNeeded(__result, ModEntry.Instance.Helper.Content.Statuses.LookupByStatus(status)?.ModOwner);

	private static IEnumerable<CodeInstruction> NewRunOptions_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldstr("ship.{0}.desc"),
					ILMatches.Ldloc<StarterShip>(originalMethod).CreateLdlocInstruction(out var ldlocStarterShip),
					ILMatches.Ldfld("ship"),
					ILMatches.Ldfld("key"),
					ILMatches.Call("FF"),
					ILMatches.Call("T")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					ldlocStarterShip,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ModDescriptions), nameof(NewRunOptions_Render_Transpiler_ModifyShipDescription)))
				)

				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static string NewRunOptions_Render_Transpiler_ModifyShipDescription(string shipDescription, StarterShip ship)
	{
		if (!Input.alt)
			return shipDescription;

		var entry = ModEntry.Instance.Helper.Content.Ships.LookupByUniqueName(ship.ship.key);
		var modName = entry is null
			? "Cobalt Core"
			: (string.IsNullOrEmpty(entry.ModOwner.DisplayName) ? entry.ModOwner.UniqueName : entry.ModOwner.DisplayName);
		return $"{shipDescription}\n{ModEntry.Instance.Localizations.Localize(["modDescription"], new { ModName = modName })}";
	}

	private static IEnumerable<CodeInstruction> Character_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Call("op_Addition"),
					ILMatches.Stloc<Vec>(originalMethod).CreateLdlocInstruction(out var ldlocPos).Anchor(out var anchor),
					ILMatches.AnyLdarg,
					ILMatches.Brtrue
				)
				.Anchors().PointerMatcher(anchor)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					ldlocPos,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ModDescriptions), nameof(Character_Render_Transpiler_AddModTooltipIfNeeded)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static void Character_Render_Transpiler_AddModTooltipIfNeeded(Character character, G g, Vec pos)
	{
		if (!Input.alt)
			return;

		var entry = character.deckType is null ? null : ModEntry.Instance.Helper.Content.Decks.LookupByDeck(character.deckType.Value);
		var modName = entry is null
			? "Cobalt Core"
			: (string.IsNullOrEmpty(entry.ModOwner.DisplayName) ? entry.ModOwner.UniqueName : entry.ModOwner.DisplayName);
		var text = ModEntry.Instance.Localizations.Localize(["modDescription"], new { ModName = modName });
		g.tooltips.Add(pos, new CustomTTText(text));
	}

	private sealed class CustomTTText : TTText
	{
		public CustomTTText(string text) : base(text)
		{
		}
	}
}

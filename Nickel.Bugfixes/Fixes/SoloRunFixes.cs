using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Bugfixes;

internal static class SoloRunFixes
{
	public static void ApplyPatches(IHarmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(DailyJustOneCharacter), nameof(DailyJustOneCharacter.OnReceiveArtifact))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(DailyJustOneCharacter)}.{nameof(DailyJustOneCharacter.OnReceiveArtifact)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(DailyJustOneCharacter_OnReceiveArtifact_Transpiler))
		);

	private static IEnumerable<CodeInstruction> DailyJustOneCharacter_OnReceiveArtifact_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Stfld("dontRemoveThese").SelectElement(out var dontRemoveTheseField, i => (FieldInfo)i.operand).Anchor(out var stfldDontRemoveTheseFieldAnchor))
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.WholeSequence, ILMatches.Ldloc(dontRemoveTheseField.Value.DeclaringType!, originalMethod).GetLocalIndex(out var capturesLocalIndex))
				.Anchors().PointerMatcher(stfldDontRemoveTheseFieldAnchor)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(DailyJustOneCharacter_OnReceiveArtifact_Transpiler_ModifyDontRemoveThese))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static List<Card> DailyJustOneCharacter_OnReceiveArtifact_Transpiler_ModifyDontRemoveThese(State state, List<Card> dontRemoveThese)
	{
		if (StarterShip.ships.TryGetValue(state.ship.key, out var starterShip))
			dontRemoveThese.AddRange(starterShip.cards);
		return dontRemoveThese;
	}
}

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

internal static class SecondOpinionsFixes
{
	public static void ApplyPatches(IHarmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(SecondOpinions), nameof(SecondOpinions.GetActions))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(SecondOpinions)}.{nameof(SecondOpinions.GetActions)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(SecondOpinions_GetActions_Transpiler))
		);

	private static IEnumerable<CodeInstruction> SecondOpinions_GetActions_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Instruction(OpCodes.Ldtoken),
					ILMatches.Call("GetTypeFromHandle"),
					ILMatches.Call("GetValues")
				)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Pop),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(SecondOpinions_GetActions_Transpiler_ModifyDeckTypes)))
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

	private static Deck[] SecondOpinions_GetActions_Transpiler_ModifyDeckTypes(State state)
	{
		IEnumerable<Card> allCards = state.deck;
		if (state.route is Combat combat)
			allCards = allCards.Concat(combat.discard).Concat(combat.exhausted).Concat(combat.hand);
		return allCards.Select(c => c.GetMeta().deck).Distinct().ToArray();
	}
}

using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Essentials;

internal static class ExhaustEntireHandActionFixes
{
	public static void ApplyPatches(IHarmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AExhaustEntireHand), nameof(AExhaustEntireHand.Begin))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(AExhaustEntireHand)}.{nameof(AExhaustEntireHand.Begin)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AExhaustEntireHand_Begin_Transpiler))
		);

	private static IEnumerable<CodeInstruction> AExhaustEntireHand_Begin_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Call(nameof(Combat.Queue)))
				.Replace(new CodeInstruction(OpCodes.Callvirt, AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.QueueImmediate), [typeof(CardAction)])))
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogInformation("Could not patch method {Method} - is `{ClassName}` no longer needed?\nReason: {Exception}", originalMethod, nameof(ExhaustEntireHandActionFixes), ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}
}

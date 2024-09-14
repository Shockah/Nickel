using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel;

internal static class BigStatsPatches
{
	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(BigStats), nameof(BigStats.ParseComboKey))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(BigStats)}.{nameof(BigStats.ParseComboKey)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(BigStats_ParseComboKey_Transpiler))
		);

	private static IEnumerable<CodeInstruction> BigStats_ParseComboKey_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Instruction(OpCodes.Ldelema),
					ILMatches.Call("TryParse")
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.Replace(
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(BigStatsPatches), nameof(BigStats_ParseComboKey_Transpiler_ParseKey)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Nickel.Instance.ModManager.Logger.LogCritical("Could not patch method {Method} - {ModLoaderName} probably won't work.\nReason: {Exception}", originalMethod, NickelConstants.Name, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static bool BigStats_ParseComboKey_Transpiler_ParseKey(string? key, out Deck result)
	{
		foreach (var deck in NewRunOptions.allChars)
		{
			if (deck.Key() == key)
			{
				result = deck;
				return true;
			}
		}
		result = default;
		return false;
	}
}

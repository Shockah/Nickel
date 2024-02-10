using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel;

internal static class CheevosPatches
{
	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Cheevos), nameof(Cheevos.TryCheevo))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Cheevos)}.{nameof(Cheevos.TryCheevo)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(TryCheevo_Transpiler))
		);
	}

	private static IEnumerable<CodeInstruction> TryCheevo_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Ldsfld("Modded").ExtractLabels(out var labels))
				.Replace(new CodeInstruction(OpCodes.Ldc_I4_0).WithLabels(labels))
				.AllElements();
		}
		catch (Exception ex)
		{
			Nickel.Instance.ModManager.Logger.LogCritical("Could not patch method {Method} - {ModLoaderName} probably won't work.\nReason: {Exception}", originalMethod, NickelConstants.Name, ex);
			return instructions;
		}
	}
}

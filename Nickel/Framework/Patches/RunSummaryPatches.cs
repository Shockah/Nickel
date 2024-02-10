using HarmonyLib;
using System;
using System.Reflection;

namespace Nickel;

internal static class RunSummaryPatches
{
	internal static bool IsDuringRunSummarySaveFromState { get; private set; } = false;

	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(RunSummary), nameof(RunSummary.SaveFromState))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(RunSummary)}.{nameof(RunSummary.SaveFromState)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(SaveFromState_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(SaveFromState_Finalizer))
		);
	}

	private static void SaveFromState_Prefix()
		=> IsDuringRunSummarySaveFromState = true;

	private static void SaveFromState_Finalizer()
		=> IsDuringRunSummarySaveFromState = false;
}

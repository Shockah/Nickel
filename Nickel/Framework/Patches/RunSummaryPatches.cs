using HarmonyLib;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Nickel;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class RunSummaryPatches
{
	internal static bool IsDuringRunSummarySaveFromState { get; private set; }

	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(RunSummary), nameof(RunSummary.SaveFromState))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(RunSummary)}.{nameof(RunSummary.SaveFromState)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(SaveFromState_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(SaveFromState_Finalizer))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(RunSummary), nameof(RunSummary.LoadOrNull))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(RunSummary)}.{nameof(RunSummary.LoadOrNull)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(LoadOrNull_Postfix))
		);
	}

	private static void SaveFromState_Prefix()
		=> IsDuringRunSummarySaveFromState = true;

	private static void SaveFromState_Finalizer()
		=> IsDuringRunSummarySaveFromState = false;

	private static void LoadOrNull_Postfix(ref RunSummary? __result)
	{
		if (__result is null)
			return;
		
		// TODO: maybe replace with blank cards/artifacts instead which still display
		__result.cards.RemoveAll(card => !DB.cards.ContainsKey(card.type));
		__result.artifacts.RemoveAll(key => !DB.artifacts.ContainsKey(key));
	}
}

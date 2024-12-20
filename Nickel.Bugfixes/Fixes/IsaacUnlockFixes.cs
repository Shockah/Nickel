using HarmonyLib;
using System;
using System.Reflection;

namespace Nickel.Bugfixes;

internal static class IsaacUnlockFixes
{
	public static void ApplyPatches(IHarmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(StoryVars), nameof(StoryVars.RecordRunWin))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(StoryVars)}.{nameof(StoryVars.RecordRunWin)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(StoryVars_RecordRunWin_Postfix))
		);

	private static void StoryVars_RecordRunWin_Postfix(StoryVars __instance)
	{
		if (FeatureFlags.BypassUnlocks)
			return;
		__instance.UnlockChar(Deck.goat);
	}
}

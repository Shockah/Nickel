using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Nickel;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class StoryVarsPatches
{
	internal static EventHandler<HashSet<Deck>>? OnGetUnlockedChars;
	internal static EventHandler<HashSet<string>>? OnGetUnlockedShips;

	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(StoryVars), nameof(StoryVars.GetUnlockedChars))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(StoryVars)}.{nameof(StoryVars.GetUnlockedChars)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetUnlockedChars_Postfix))
		);

		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(StoryVars), nameof(StoryVars.GetUnlockedShips))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(StoryVars)}.{nameof(StoryVars.GetUnlockedShips)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetUnlockedShips_Postfix))
		);
	}

	private static void GetUnlockedChars_Postfix(ref HashSet<Deck> __result)
		=> OnGetUnlockedChars?.Invoke(null, __result);

	private static void GetUnlockedShips_Postfix(ref HashSet<string> __result)
		=> OnGetUnlockedShips?.Invoke(null, __result);
}

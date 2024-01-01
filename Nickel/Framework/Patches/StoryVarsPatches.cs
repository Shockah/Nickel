using HarmonyLib;
using System;
using System.Collections.Generic;
using WeakEvent;

namespace Nickel;

internal static class StoryVarsPatches
{
	internal static WeakEventSource<HashSet<Deck>> OnGetUnlockedChars { get; } = new();
	internal static WeakEventSource<HashSet<string>> OnGetUnlockedShips { get; } = new();

	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(StoryVars), nameof(StoryVars.GetUnlockedChars))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(StoryVars)}.{nameof(StoryVars.GetUnlockedChars)}`"),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(StoryVarsPatches), nameof(GetUnlockedChars_Postfix)))
		);

		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(StoryVars), nameof(StoryVars.GetUnlockedShips))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(StoryVars)}.{nameof(StoryVars.GetUnlockedShips)}`"),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(StoryVarsPatches), nameof(GetUnlockedShips_Postfix)))
		);
	}

	private static void GetUnlockedChars_Postfix(ref HashSet<Deck> __result)
		=> OnGetUnlockedChars.Raise(null, __result);

	private static void GetUnlockedShips_Postfix(ref HashSet<string> __result)
		=> OnGetUnlockedShips.Raise(null, __result);
}

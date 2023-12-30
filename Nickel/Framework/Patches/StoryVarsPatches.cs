using HarmonyLib;
using System;
using System.Collections.Generic;
using WeakEvent;

namespace Nickel;

internal static class StoryVarsPatches
{
	internal static WeakEventSource<HashSet<Deck>> OnGetUnlockedChars { get; } = new();

	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(StoryVars), nameof(StoryVars.GetUnlockedChars)) ?? throw new InvalidOperationException("Could not patch game methods: missing method `StoryVars.GetUnlockedChars`"),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(StoryVarsPatches), nameof(GetUnlockedChars_Postfix)))
		);
	}

	private static void GetUnlockedChars_Postfix(ref HashSet<Deck> __result)
		=> OnGetUnlockedChars.Raise(null, __result);
}

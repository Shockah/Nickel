using HarmonyLib;
using System;

namespace Nickel;

internal static class MGPatches
{
	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(MG), "DrawLoadingScreen")
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(MG)}.DrawLoadingScreen`"),
			prefix: new HarmonyMethod(typeof(MGPatches), nameof(DrawLoadingScreen_Prefix)),
			postfix: new HarmonyMethod(typeof(MGPatches), nameof(DrawLoadingScreen_Postfix))
		);
	}

	private static void DrawLoadingScreen_Prefix(MG __instance, ref int __state)
		=> __state = __instance.loadingQueue?.Count ?? 0;

	private static void DrawLoadingScreen_Postfix(MG __instance, ref int __state)
	{
		if (__state <= 0)
			return;
		if ((__instance.loadingQueue?.Count ?? 0) > 0)
			return;
		Nickel.Instance.ModManager.LoadMods(ModLoadPhase.AfterDbInit);
	}
}

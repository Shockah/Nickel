using HarmonyLib;
using System;

namespace Nickel;

internal static class GenericKeyPatches
{
	internal static void Apply<T>(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(T), "Key")
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(T)}.Key`"),
			postfix: new HarmonyMethod(typeof(CardPatches), nameof(Key_Postfix))
		);
	}

	private static void Key_Postfix(object __instance, ref string __result)
		=> __result = __instance.GetType().FullName ?? __instance.GetType().Name;
}

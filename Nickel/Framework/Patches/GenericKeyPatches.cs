using HarmonyLib;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Nickel;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class GenericKeyPatches
{
	internal static void Apply<T>(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(T), "Key")
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(T)}.Key`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Key_Postfix))
		);

	private static void Key_Postfix(object __instance, ref string __result)
	{
		if (__instance.GetType().Assembly != typeof(G).Assembly)
			__result = __instance.GetType().FullName ?? __instance.GetType().Name;
	}
}

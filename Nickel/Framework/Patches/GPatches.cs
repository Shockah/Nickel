using HarmonyLib;
using System;
using System.Reflection;

namespace Nickel;

internal static class GPatches
{
	internal static EventHandler<G>? OnAfterFrame;
	
	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(G), nameof(G.OnAfterFrame))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(G)}.{nameof(G.OnAfterFrame)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(OnAfterFrame_Prefix))
		);

	private static void OnAfterFrame_Prefix(G __instance)
		=> OnAfterFrame?.Invoke(null, __instance);
}

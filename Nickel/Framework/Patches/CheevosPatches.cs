using HarmonyLib;
using System;
using System.Reflection;

namespace Nickel;

internal static class CheevosPatches
{
	internal static EventHandler<State>? OnCheckOnLoad;
	
	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Cheevos), nameof(Cheevos.CheckOnLoad))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Cheevos)}.{nameof(Cheevos.CheckOnLoad)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CheckOnLoad_Prefix))
		);

	private static void CheckOnLoad_Prefix(State s)
		=> OnCheckOnLoad?.Invoke(null, s);
}

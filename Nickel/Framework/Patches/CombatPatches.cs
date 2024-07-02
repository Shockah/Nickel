using HarmonyLib;
using System;
using System.Reflection;
using WeakEvent;

namespace Nickel;

internal static class CombatPatches
{
	internal static readonly WeakEventSource<State> OnReturnCardsToDeck = new();

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.ReturnCardsToDeck))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Combat)}.{nameof(Combat.ReturnCardsToDeck)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(ReturnCardsToDeck_Postfix))
		);

	private static void ReturnCardsToDeck_Postfix(State state)
		=> OnReturnCardsToDeck.Raise(null, state);
}

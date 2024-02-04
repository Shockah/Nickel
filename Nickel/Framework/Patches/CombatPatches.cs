using HarmonyLib;
using WeakEvent;

namespace Nickel;

internal static class CombatPatches
{
	internal static WeakEventSource<ReturnCardsToDeckEventArgs> OnReturnCardsToDeck { get; } = new();

	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Combat), nameof(Combat.ReturnCardsToDeck)),
			postfix: new HarmonyMethod(typeof(CombatPatches), nameof(ReturnCardsToDeck_Postfix))
		);
	}

	private static void ReturnCardsToDeck_Postfix(State state)
	{
		var eventArgs = new ReturnCardsToDeckEventArgs { State = state };
		OnReturnCardsToDeck.Raise(null, eventArgs);
	}

	internal sealed class ReturnCardsToDeckEventArgs
	{
		public required State State { get; init; }
	}
}

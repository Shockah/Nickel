using HarmonyLib;
using System;
using System.Reflection;
using WeakEvent;

namespace Nickel;

internal static class CardPatches
{
	internal static WeakEventSource<KeyEventArgs> OnKey { get; } = new();

	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Key))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Card)}.{nameof(Card.Key)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Key_Postfix))
		);
	}

	private static void Key_Postfix(Card __instance, ref string __result)
	{
		var eventArgs = new KeyEventArgs { Card = __instance, Key = __result };
		OnKey.Raise(null, eventArgs);
		__result = eventArgs.Key;
	}

	internal sealed class KeyEventArgs
	{
		public required Card Card { get; init; }
		public required string Key { get; set; }
	}
}

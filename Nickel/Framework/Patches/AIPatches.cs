using HarmonyLib;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using WeakEvent;

namespace Nickel;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class AIPatches
{
	internal static WeakEventSource<KeyEventArgs> OnKey { get; } = new();

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AI), nameof(AI.Key))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(AI)}.{nameof(AI.Key)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Key_Postfix))
		);

	private static void Key_Postfix(AI __instance, ref string __result)
	{
		var eventArgs = new KeyEventArgs { AI = __instance, Key = __result };
		OnKey.Raise(null, eventArgs);
		__result = eventArgs.Key;
	}

	internal sealed class KeyEventArgs
	{
		public required AI AI { get; init; }
		public required string Key { get; set; }
	}
}

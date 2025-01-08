using HarmonyLib;
using System;
using System.Reflection;

namespace Nickel;

internal static class AIPatches
{
	internal static RefEventHandler<KeyEventArgs>? OnKey;

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AI), nameof(AI.Key))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(AI)}.{nameof(AI.Key)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Key_Postfix))
		);

	private static void Key_Postfix(AI __instance, ref string __result)
	{
		var args = new KeyEventArgs
		{
			AI = __instance,
			Key = __result,
		};
		OnKey?.Invoke(null, ref args);
		__result = args.Key;
	}

	internal struct KeyEventArgs
	{
		public required AI AI { get; init; }
		public required string Key;
	}
}

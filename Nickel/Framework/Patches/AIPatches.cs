using HarmonyLib;
using System;
using System.Reflection;

namespace Nickel;

internal static class AIPatches
{
	internal static EventHandler<KeyEventArgs>? OnKey;

	private static readonly Pool<KeyEventArgs> KeyEventArgsPool = new(() => new());

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AI), nameof(AI.Key))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(AI)}.{nameof(AI.Key)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Key_Postfix))
		);

	private static void Key_Postfix(AI __instance, ref string __result)
	{
		var result = __result;
		KeyEventArgsPool.Do(args =>
		{
			args.AI = __instance;
			args.Key = result;
			OnKey?.Invoke(null, args);
			result = args.Key;
		});
		__result = result;
	}

	internal sealed class KeyEventArgs
	{
		public AI AI { get; internal set; } = null!;
		public string Key { get; set; } = null!;
	}
}

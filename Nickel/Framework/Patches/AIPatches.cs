using HarmonyLib;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Nickel;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class AIPatches
{
	internal static EventHandler<KeyEventArgs>? OnKey;
	
	private static readonly KeyEventArgs KeyEventArgsInstance = new();

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AI), nameof(AI.Key))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(AI)}.{nameof(AI.Key)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Key_Postfix))
		);

	private static void Key_Postfix(AI __instance, ref string __result)
	{
		var args = KeyEventArgsInstance;
		args.AI = __instance;
		args.Key = __result;
		OnKey?.Invoke(null, args);
		__result = args.Key;
	}

	internal sealed class KeyEventArgs
	{
		public AI AI { get; internal set; } = null!;
		public string Key { get; set; } = null!;
	}
}

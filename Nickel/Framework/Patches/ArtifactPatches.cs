using HarmonyLib;
using System;
using System.Reflection;

namespace Nickel;

internal static class ArtifactPatches
{
	internal static RefEventHandler<KeyEventArgs>? OnKey;

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Artifact), nameof(Artifact.Key))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Artifact)}.{nameof(Artifact.Key)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Key_Postfix))
		);

	private static void Key_Postfix(Artifact __instance, ref string __result)
	{
		var args = new KeyEventArgs
		{
			Artifact = __instance,
			Key = __result,
		};
		OnKey?.Invoke(null, ref args);
		__result = args.Key;
	}

	internal struct KeyEventArgs
	{
		public required Artifact Artifact { get; init; }
		public required string Key;
	}
}

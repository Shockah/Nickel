using HarmonyLib;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using WeakEvent;

namespace Nickel;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class ArtifactPatches
{
	internal static WeakEventSource<KeyEventArgs> OnKey { get; } = new();

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Artifact), nameof(Artifact.Key))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Artifact)}.{nameof(Artifact.Key)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Key_Postfix))
		);

	private static void Key_Postfix(Artifact __instance, ref string __result)
	{
		var eventArgs = new KeyEventArgs { Artifact = __instance, Key = __result };
		OnKey.Raise(null, eventArgs);
		__result = eventArgs.Key;
	}

	internal sealed class KeyEventArgs
	{
		public required Artifact Artifact { get; init; }
		public required string Key { get; set; }
	}
}

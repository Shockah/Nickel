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
	
	private static readonly KeyEventArgs KeyEventArgsInstance = new();

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Artifact), nameof(Artifact.Key))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Artifact)}.{nameof(Artifact.Key)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Key_Postfix))
		);

	private static void Key_Postfix(Artifact __instance, ref string __result)
	{
		var args = KeyEventArgsInstance;
		args.Artifact = __instance;
		args.Key = __result;
		OnKey.Raise(null, args);
		__result = args.Key;
	}

	internal sealed class KeyEventArgs
	{
		public Artifact Artifact { get; internal set; } = null!;
		public string Key { get; set; } = null!;
	}
}

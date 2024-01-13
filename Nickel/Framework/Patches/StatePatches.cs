using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using WeakEvent;

namespace Nickel;

internal static class StatePatches
{
	internal static bool StopSavingOverride = false;
	internal static WeakEventSource<EnumerateAllArtifactsEventArgs> OnEnumerateAllArtifacts { get; } = new();
	internal static WeakEventSource<LoadEventArgs> OnLoad { get; } = new();

	internal static void Apply(Harmony harmony, bool saveInDebug)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.EnumerateAllArtifacts))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(State)}.{nameof(State.EnumerateAllArtifacts)}`"),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(StatePatches), nameof(EnumerateAllArtifacts_Postfix)), priority: Priority.Last)
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.SaveIfRelease))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(State)}.{nameof(State.SaveIfRelease)}`"),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(StatePatches), nameof(SaveIfRelease_Prefix))),
			postfix: saveInDebug ? new HarmonyMethod(AccessTools.DeclaredMethod(typeof(StatePatches), nameof(SaveIfRelease_Postfix))) : null
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.Load))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(State)}.{nameof(State.Load)}`"),
			postfix: new HarmonyMethod(typeof(StatePatches), nameof(Load_Postfix))
		);
	}

	private static void EnumerateAllArtifacts_Postfix(State __instance, ref List<Artifact> __result)
	{
		var eventArgs = new EnumerateAllArtifactsEventArgs { State = __instance, Artifacts = __result.ToList() };
		OnEnumerateAllArtifacts.Raise(null, eventArgs);
		__result = eventArgs.Artifacts;
	}

	private static bool SaveIfRelease_Prefix()
		=> !StopSavingOverride;

	private static void SaveIfRelease_Postfix(State __instance)
	{
		if (FeatureFlags.Debug && !StopSavingOverride)
			__instance.Save();
	}

	private static void Load_Postfix(ref State.SaveSlot __result)
	{
		if (__result.state is not { } state)
			return;
		var eventArgs = new LoadEventArgs { State = state, IsCorrupted = __result.isCorrupted };
		OnLoad.Raise(null, eventArgs);
		__result.isCorrupted = eventArgs.IsCorrupted;
	}

	internal sealed class EnumerateAllArtifactsEventArgs
	{
		public required State State { get; init; }
		public required List<Artifact> Artifacts { get; set; }
	}

	internal sealed class LoadEventArgs
	{
		public required State State { get; init; }
		public required bool IsCorrupted { get; set; }
	}
}

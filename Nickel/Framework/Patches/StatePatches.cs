using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using WeakEvent;

namespace Nickel;

internal static class StatePatches
{
	internal static WeakEventSource<EnumerateAllArtifactsEventArgs> OnEnumerateAllArtifacts { get; } = new();

	internal static void Apply(Harmony harmony, bool saveInDebug)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.EnumerateAllArtifacts))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(State)}.{nameof(State.EnumerateAllArtifacts)}`"),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(StatePatches), nameof(EnumerateAllArtifacts_Postfix)), priority: Priority.Last)
		);

		if (saveInDebug)
			harmony.Patch(
				original: AccessTools.DeclaredMethod(typeof(State), nameof(State.SaveIfRelease))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(State)}.{nameof(State.SaveIfRelease)}`"),
				postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(StatePatches), nameof(SaveIfRelease_Postfix)))
			);
	}

	private static void EnumerateAllArtifacts_Postfix(State __instance, ref List<Artifact> __result)
	{
		var eventArgs = new EnumerateAllArtifactsEventArgs { State = __instance, Artifacts = __result.ToList() };
		OnEnumerateAllArtifacts.Raise(null, eventArgs);
		__result = eventArgs.Artifacts;
	}

	private static void SaveIfRelease_Postfix(State __instance)
	{
		if (FeatureFlags.Debug)
			__instance.Save();
	}

	internal sealed class EnumerateAllArtifactsEventArgs
	{
		public required State State { get; init; }
		public required List<Artifact> Artifacts { get; set; }
	}
}

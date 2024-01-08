using HarmonyLib;
using Nanoray.Shrike;
using System;
using System.Collections.Generic;
using System.Linq;
using WeakEvent;

namespace Nickel;

internal static class StatePatches
{
	internal static WeakEventSource<ObjectRef<List<Artifact>>> OnEnumerateAllArtifacts { get; } = new();

	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.EnumerateAllArtifacts))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(State)}.{nameof(State.EnumerateAllArtifacts)}`"),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(StatePatches), nameof(EnumerateAllArtifacts_Postfix)), priority: Priority.Last)
		);
	}

	private static void EnumerateAllArtifacts_Postfix(ref List<Artifact> __result)
	{
		var eventArgs = new ObjectRef<List<Artifact>>(__result.ToList());
		OnEnumerateAllArtifacts.Raise(null, eventArgs);
		__result = eventArgs.Value;
	}
}

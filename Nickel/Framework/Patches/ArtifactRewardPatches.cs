using HarmonyLib;
using System;
using System.Collections.Generic;
using WeakEvent;

namespace Nickel;

internal static class ArtifactRewardPatches
{
	internal static WeakEventSource<GetBlockedArtifactsEventArgs> OnGetBlockedArtifacts { get; } = new();

	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ArtifactReward), "GetBlockedArtifacts")
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(ArtifactReward)}.GetBlockedArtifacts`"),
			postfix: new HarmonyMethod(typeof(ArtifactRewardPatches), nameof(GetBlockedArtifacts_Postfix))
		);
	}

	private static void GetBlockedArtifacts_Postfix(State s, ref HashSet<Type> __result)
		=> OnGetBlockedArtifacts.Raise(null, new() { State = s, BlockedArtifacts = __result });

	internal sealed class GetBlockedArtifactsEventArgs
	{
		public required State State { get; init; }
		public required HashSet<Type> BlockedArtifacts { get; init; }
	}
}

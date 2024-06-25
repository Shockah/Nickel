using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using WeakEvent;

namespace Nickel;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class ArtifactRewardPatches
{
	internal static WeakEventSource<GetBlockedArtifactsEventArgs> OnGetBlockedArtifacts { get; } = new();

	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(ArtifactReward), nameof(ArtifactReward.GetBlockedArtifacts))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(ArtifactReward)}.{nameof(ArtifactReward.GetBlockedArtifacts)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetBlockedArtifacts_Postfix))
		);
		harmony.Patch(
			original: typeof(ArtifactReward).GetNestedTypes(AccessTools.all).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<GetOffering>") && m.ReturnType == typeof(bool))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(ArtifactReward)}.{nameof(ArtifactReward.GetOffering)}.<Where delegate>`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetOffering_Delegate_Transpiler))
		);
	}

	private static void GetBlockedArtifacts_Postfix(State s, ref HashSet<Type> __result)
		=> OnGetBlockedArtifacts.Raise(null, new() { State = s, BlockedArtifacts = __result });

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> GetOffering_Delegate_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldsfld("artifactMetas"),
					ILMatches.Ldarg(1),
					ILMatches.Call("get_Name")
				)
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.Replace(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetOffering_Delegate_Transpiler_GetKey))))
				.AllElements();
		}
		catch (Exception ex)
		{
			Nickel.Instance.ModManager.Logger.LogCritical("Could not patch method {Method} - {ModLoaderName} probably won't work.\nReason: {Exception}", originalMethod, NickelConstants.Name, ex);
			return instructions;
		}
	}

	private static string GetOffering_Delegate_Transpiler_GetKey(Type type)
		=> ((Artifact)Activator.CreateInstance(type)!).Key();

	internal sealed class GetBlockedArtifactsEventArgs
	{
		public required State State { get; init; }
		public required HashSet<Type> BlockedArtifacts { get; init; }
	}
}

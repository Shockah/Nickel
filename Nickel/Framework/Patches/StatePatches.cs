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
internal static class StatePatches
{
	internal static bool StopSavingOverride = false;
	internal static WeakEventSource<EnumerateAllArtifactsEventArgs> OnEnumerateAllArtifacts { get; } = new();
	internal static WeakEventSource<ModifyPotentialExeCardsEventArgs> OnModifyPotentialExeCards { get; } = new();
	internal static WeakEventSource<LoadEventArgs> OnLoad { get; } = new();
	internal static WeakEventSource<State> OnUpdate { get; } = new();

	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.EnumerateAllArtifacts))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(State)}.{nameof(State.EnumerateAllArtifacts)}`"),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(EnumerateAllArtifacts_Postfix)), priority: Priority.Last)
		);
		harmony.Patch(
			original: typeof(State).GetNestedTypes(AccessTools.all).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<PopulateRun>") && m.ReturnType == typeof(Route))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(State)}.<compiler-generated-type>.<PopulateRun>`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_PopulateRun_Delegate_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.SaveIfRelease))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(State)}.{nameof(State.SaveIfRelease)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(SaveIfRelease_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(SaveIfRelease_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.Load))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(State)}.{nameof(State.Load)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Load_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.Update))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(State)}.{nameof(State.Update)}`"),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Update_Prefix)), priority: Priority.First)
		);
	}

	private static void EnumerateAllArtifacts_Postfix(State __instance, ref List<Artifact> __result)
	{
		var eventArgs = new EnumerateAllArtifactsEventArgs { State = __instance, Artifacts = __result.ToList() };
		OnEnumerateAllArtifacts.Raise(null, eventArgs);
		__result = eventArgs.Artifacts;
	}

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> State_PopulateRun_Delegate_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("chars"),
					ILMatches.LdcI4((int)Deck.shard),
					ILMatches.Call("Contains"),
					ILMatches.Brtrue,
					ILMatches.Ldloc<List<Card>>(originalMethod).CreateLdlocInstruction(out var ldlocCards),
					ILMatches.Instruction(OpCodes.Newobj),
					ILMatches.Call("Add")
				)
				.PointerMatcher(SequenceMatcherRelativeElement.AfterLast)
				.ExtractLabels(out var labels)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(originalMethod.DeclaringType, "chars")),
					ldlocCards,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(StatePatches), nameof(State_PopulateRun_Delegate_Transpiler_ModifyPotentialExeCards)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Nickel.Instance.ModManager.Logger.LogCritical("Could not patch method {Method} - {ModLoaderName} probably won't work.\nReason: {Exception}", originalMethod, NickelConstants.Name, ex);
			return instructions;
		}
	}

	private static void State_PopulateRun_Delegate_Transpiler_ModifyPotentialExeCards(IEnumerable<Deck> chars, List<Card> cards)
	{
		var eventArgs = new ModifyPotentialExeCardsEventArgs { Characters = chars.ToHashSet(), ExeCards = cards };
		OnModifyPotentialExeCards.Raise(null, eventArgs);
	}

	private static bool SaveIfRelease_Prefix()
		=> !StopSavingOverride;

	private static void SaveIfRelease_Postfix(State __instance)
	{
		if (Nickel.Instance.DebugMode != DebugMode.EnabledWithSaving)
			return;
		if (FeatureFlags.Debug && !StopSavingOverride)
			__instance.Save();
	}

	private static void Load_Postfix(int slot, ref State.SaveSlot __result)
	{
		var eventArgs = new LoadEventArgs { Slot = slot, Data = __result };
		OnLoad.Raise(null, eventArgs);
		__result = eventArgs.Data;
	}

	private static void Update_Prefix(State __instance)
		=> OnUpdate.Raise(null, __instance);

	internal sealed class EnumerateAllArtifactsEventArgs
	{
		public required State State { get; init; }
		public required List<Artifact> Artifacts { get; set; }
	}

	internal sealed class ModifyPotentialExeCardsEventArgs
	{
		public required HashSet<Deck> Characters { get; init; }
		public required List<Card> ExeCards { get; init; }
	}

	internal sealed class LoadEventArgs
	{
		public required int Slot { get; init; }
		public required State.SaveSlot Data { get; set; }
	}
}

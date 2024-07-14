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
	
	private static readonly EnumerateAllArtifactsEventArgs EnumerateAllArtifactsEventArgsInstance = new();
	private static readonly ModifyPotentialExeCardsEventArgs ModifyPotentialExeCardsEventArgsInstance = new();
	private static readonly LoadEventArgs LoadEventArgsInstance = new();

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
		var args = EnumerateAllArtifactsEventArgsInstance;
		args.State = __instance;
		args.Artifacts = __result;
		OnEnumerateAllArtifacts.Raise(null, args);
		__result = args.Artifacts;
	}

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> State_PopulateRun_Delegate_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("chars"),
					ILMatches.LdcI4((int)Deck.shard),
					ILMatches.Call("Contains"),
					ILMatches.Brtrue,
					ILMatches.Ldloc<List<Card>>(originalMethod).CreateLdlocaInstruction(out var ldlocaCards),
					ILMatches.Instruction(OpCodes.Newobj),
					ILMatches.Call("Add")
				])
				.PointerMatcher(SequenceMatcherRelativeElement.AfterLast)
				.ExtractLabels(out var labels)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(originalMethod.DeclaringType, "chars")),
					ldlocaCards,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(StatePatches), nameof(State_PopulateRun_Delegate_Transpiler_ModifyPotentialExeCards)))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			Nickel.Instance.ModManager.Logger.LogCritical("Could not patch method {Method} - {ModLoaderName} probably won't work.\nReason: {Exception}", originalMethod, NickelConstants.Name, ex);
			return instructions;
		}
	}

	private static void State_PopulateRun_Delegate_Transpiler_ModifyPotentialExeCards(IEnumerable<Deck> chars, ref List<Card> cards)
	{
		var args = ModifyPotentialExeCardsEventArgsInstance;
		args.Characters = chars.ToHashSet();
		args.ExeCards = cards;
		OnModifyPotentialExeCards.Raise(null, args);
		cards = args.ExeCards;
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
		var args = LoadEventArgsInstance;
		args.Slot = slot;
		args.Data = __result;
		OnLoad.Raise(null, args);
		__result = args.Data;
	}

	private static void Update_Prefix(State __instance)
		=> OnUpdate.Raise(null, __instance);

	internal sealed class EnumerateAllArtifactsEventArgs
	{
		public State State { get; internal set; } = null!;
		public List<Artifact> Artifacts { get; set; } = null!;
	}

	internal sealed class ModifyPotentialExeCardsEventArgs
	{
		public HashSet<Deck> Characters { get; internal set; } = null!;
		public List<Card> ExeCards { get; set; } = null!;
	}

	internal sealed class LoadEventArgs
	{
		public int Slot { get; internal set; }
		public State.SaveSlot Data { get; set; } = null!;
	}
}

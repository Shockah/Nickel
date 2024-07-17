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

namespace Nickel;

internal static class HarmonyPatches
{
	private static PatchInfo? CurrentPatchInfo;
	internal static readonly Stack<DelayedHarmonyManager> DelayedManagerStack = [];

	private static readonly Lazy<Type> PatchJobsMethodInfoJobType = new(() => AccessTools.Inner(typeof(Harmony).Assembly.GetType("HarmonyLib.PatchJobs")!.MakeGenericType([typeof(MethodInfo)]), "Job"));
	private static readonly Lazy<Func<object, MethodBase>> GetOriginal = new(() => CreateFieldGetter<MethodBase>(PatchJobsMethodInfoJobType.Value, "original"));
	private static readonly Lazy<Func<object, List<HarmonyMethod>>> GetPrefixes = new(() => CreateFieldGetter<List<HarmonyMethod>>(PatchJobsMethodInfoJobType.Value, "prefixes"));
	private static readonly Lazy<Func<object, List<HarmonyMethod>>> GetPostfixes = new(() => CreateFieldGetter<List<HarmonyMethod>>(PatchJobsMethodInfoJobType.Value, "postfixes"));
	private static readonly Lazy<Func<object, List<HarmonyMethod>>> GetTranspilers = new(() => CreateFieldGetter<List<HarmonyMethod>>(PatchJobsMethodInfoJobType.Value, "transpilers"));
	private static readonly Lazy<Func<object, List<HarmonyMethod>>> GetFinalizers = new(() => CreateFieldGetter<List<HarmonyMethod>>(PatchJobsMethodInfoJobType.Value, "finalizers"));
	private static readonly Lazy<Func<object, Harmony>> GetHarmonyInstance = new(() => CreateFieldGetter<Harmony>(typeof(PatchClassProcessor), "instance"));

	private static Func<object, T> CreateFieldGetter<T>(Type type, string fieldName)
	{
		var field = AccessTools.DeclaredField(type, fieldName);
		var method = new DynamicMethod($"get_{fieldName}", typeof(T), [type]);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldfld, field);
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Func<object, T>>();
	}

	internal static void Apply(Harmony harmony, ILogger logger)
	{
		logger.LogInformation("Preparing Harmony for mod usage...");
		PatchUpdateWrapper();
		PatchCreateDynamicMethod();
		PatchClassProcessorProcessPatchJob();

		void PatchUpdateWrapper()
		{
			if (AccessTools.DeclaredMethod(typeof(Harmony).Assembly.GetType("HarmonyLib.PatchFunctions"), "UpdateWrapper") is not { } originalMethod)
			{
				logger.LogError("Could not patch Harmony methods for better debugging capabilities: missing method.");
				return;
			}

			harmony.Patch(
				original: originalMethod,
				prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(PatchFunctions_UpdateWrapper_Prefix)),
				postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(PatchFunctions_UpdateWrapper_Postfix))
			);
		}

		void PatchCreateDynamicMethod()
		{
			if (AccessTools.DeclaredMethod(typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcher"), "CreateDynamicMethod") is not { } originalMethod)
			{
				logger.LogError("Could not patch Harmony methods for better debugging capabilities: missing method.");
				return;
			}

			harmony.Patch(
				original: originalMethod,
				prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(MethodPatcher_CreateDynamicMethod_Prefix))
			);
		}

		void PatchClassProcessorProcessPatchJob()
		{
			if (AccessTools.DeclaredMethod(typeof(Harmony).Assembly.GetType("HarmonyLib.PatchClassProcessor"), "ProcessPatchJob") is not { } originalMethod)
			{
				logger.LogError("Could not patch Harmony methods for batching up patches: missing method.");
				return;
			}

			harmony.Patch(
				original: originalMethod,
				transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(PatchClassProcessor_ProcessPatchJob_Transpiler))
			);
		}
	}

	private static void PatchFunctions_UpdateWrapper_Prefix(PatchInfo patchInfo)
		=> CurrentPatchInfo = patchInfo;

	private static void PatchFunctions_UpdateWrapper_Postfix()
		=> CurrentPatchInfo = null;

	private static void MethodPatcher_CreateDynamicMethod_Prefix(ref string suffix)
	{
		if (CurrentPatchInfo is null)
			return;

		var owners = CurrentPatchInfo.prefixes
			.Concat(CurrentPatchInfo.postfixes)
			.Concat(CurrentPatchInfo.finalizers)
			.Concat(CurrentPatchInfo.transpilers)
			.Select(p => p.owner)
			.Distinct()
			.ToList();

		if (owners.Count == 0)
		{
			suffix = "_Unpatched";
			return;
		}

		suffix = $"_Patch_{string.Join("_", owners.Select(o => $"<{o}>"))}";
	}

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> PatchClassProcessor_ProcessPatchJob_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Call("RunMethod"),
					ILMatches.Stloc<bool>(originalMethod).GetLocalIndex(out var individualPrepareResultLocalIndex),
				])
				.Find([
					ILMatches.Ldloc(individualPrepareResultLocalIndex),
					ILMatches.Brfalse,
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					new CodeInstruction(OpCodes.Ldloca, individualPrepareResultLocalIndex.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(PatchClassProcessor_ProcessPatchJob_Transpiler_ModifyIndividualPrepareResult)))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			Nickel.Instance.ModManager.Logger.LogCritical("Could not patch method {Method} - {ModLoaderName} probably won't work.\nReason: {Exception}", originalMethod, NickelConstants.Name, ex);
			return instructions;
		}
	}

	private static void PatchClassProcessor_ProcessPatchJob_Transpiler_ModifyIndividualPrepareResult(object processor, object job, ref bool individualPrepareResult)
	{
		if (!DelayedManagerStack.TryPeek(out var manager))
			return;

		var harmony = GetHarmonyInstance.Value(processor);
		var original = GetOriginal.Value(job);
		
		foreach (var prefix in GetPrefixes.Value(job))
			manager.Patch(harmony.Id, original, prefix: prefix);
		foreach (var postfix in GetPostfixes.Value(job))
			manager.Patch(harmony.Id, original, postfix: postfix);
		foreach (var transpiler in GetTranspilers.Value(job))
			manager.Patch(harmony.Id, original, transpiler: transpiler);
		foreach (var finalizer in GetFinalizers.Value(job))
			manager.Patch(harmony.Id, original, finalizer: finalizer);
		individualPrepareResult = false;
	}
}

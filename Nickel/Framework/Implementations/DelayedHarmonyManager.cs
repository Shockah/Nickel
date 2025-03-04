using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Nickel;

internal sealed class DelayedHarmonyManager
{
	private static readonly Lazy<Func<object>> GetLocker = new(() => AccessTools.DeclaredField(typeof(PatchProcessor), "locker").EmitStaticGetter<object>());
	
	private static readonly Lazy<Func<MethodBase, PatchInfo?>> GetPatchInfo = new(() =>
	{
		var method = new DynamicMethod("GetHarmonySharedStatePatchInfo", typeof(PatchInfo), [typeof(MethodBase)]);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Harmony).Assembly.GetType("HarmonyLib.HarmonySharedState"), "GetPatchInfo", [typeof(MethodBase)]));
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Func<MethodBase, PatchInfo>>();
	});
	
	private static readonly Lazy<Func<MethodBase, PatchInfo, MethodInfo>> UpdateWrapper = new(() =>
	{
		var method = new DynamicMethod("UpdateWrappedPatchFunction", typeof(MethodInfo), [typeof(MethodBase), typeof(PatchInfo)]);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldarg_1);
		il.Emit(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Harmony).Assembly.GetType("HarmonyLib.PatchFunctions"), "UpdateWrapper", [typeof(MethodBase), typeof(PatchInfo)]));
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Func<MethodBase, PatchInfo, MethodInfo>>();
	});
	
	private static readonly Lazy<Action<MethodBase, MethodInfo, PatchInfo>> UpdatePatchInfo = new(() =>
	{
		var method = new DynamicMethod("UpdateHarmonySharedStatePatchInfo", typeof(void), [typeof(MethodBase), typeof(MethodInfo), typeof(PatchInfo)]);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldarg_1);
		il.Emit(OpCodes.Ldarg_2);
		il.Emit(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Harmony).Assembly.GetType("HarmonyLib.HarmonySharedState"), "UpdatePatchInfo", [typeof(MethodBase), typeof(MethodInfo), typeof(PatchInfo)]));
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Action<MethodBase, MethodInfo, PatchInfo>>();
	});
	
	private static readonly Lazy<Action<PatchInfo, string, HarmonyMethod[]>> AddPrefixes = new(() =>
	{
		var method = new DynamicMethod("AddPatchInfoPrefixes", typeof(void), [typeof(PatchInfo), typeof(string), typeof(HarmonyMethod[])]);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldarg_1);
		il.Emit(OpCodes.Ldarg_2);
		il.Emit(OpCodes.Call, AccessTools.DeclaredMethod(typeof(PatchInfo), "AddPrefixes", [typeof(string), typeof(HarmonyMethod[])]));
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Action<PatchInfo, string, HarmonyMethod[]>>();
	});
	
	private static readonly Lazy<Action<PatchInfo, string, HarmonyMethod[]>> AddPostfixes = new(() =>
	{
		var method = new DynamicMethod("AddPatchInfoPostfixes", typeof(void), [typeof(PatchInfo), typeof(string), typeof(HarmonyMethod[])]);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldarg_1);
		il.Emit(OpCodes.Ldarg_2);
		il.Emit(OpCodes.Call, AccessTools.DeclaredMethod(typeof(PatchInfo), "AddPostfixes", [typeof(string), typeof(HarmonyMethod[])]));
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Action<PatchInfo, string, HarmonyMethod[]>>();
	});
	
	private static readonly Lazy<Action<PatchInfo, string, HarmonyMethod[]>> AddTranspilers = new(() =>
	{
		var method = new DynamicMethod("AddPatchInfoTranspilers", typeof(void), [typeof(PatchInfo), typeof(string), typeof(HarmonyMethod[])]);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldarg_1);
		il.Emit(OpCodes.Ldarg_2);
		il.Emit(OpCodes.Call, AccessTools.DeclaredMethod(typeof(PatchInfo), "AddTranspilers", [typeof(string), typeof(HarmonyMethod[])]));
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Action<PatchInfo, string, HarmonyMethod[]>>();
	});
	
	private static readonly Lazy<Action<PatchInfo, string, HarmonyMethod[]>> AddFinalizers = new(() =>
	{
		var method = new DynamicMethod("AddPatchInfoFinalizers", typeof(void), [typeof(PatchInfo), typeof(string), typeof(HarmonyMethod[])]);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldarg_1);
		il.Emit(OpCodes.Ldarg_2);
		il.Emit(OpCodes.Call, AccessTools.DeclaredMethod(typeof(PatchInfo), "AddFinalizers", [typeof(string), typeof(HarmonyMethod[])]));
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Action<PatchInfo, string, HarmonyMethod[]>>();
	});

	private readonly Dictionary<MethodBase, Dictionary<string, DelayedPatches>> Patches = [];

	public void Patch(string id, MethodBase original, HarmonyMethod? prefix = null, HarmonyMethod? postfix = null, HarmonyMethod? transpiler = null, HarmonyMethod? finalizer = null)
	{
		// exceptions from https://github.com/pardeike/Harmony/blob/master/Harmony/Public/PatchProcessor.cs
		
		if (original is null)
			throw new NullReferenceException($"Null method for {id}");

		if (!original.IsDeclaredMember())
		{
			var declaredMember = original.GetDeclaredMember();
			throw new ArgumentException($"You can only patch implemented methods/constructors. Patch the declared method {declaredMember.FullDescription()} instead.");
		}

		if (prefix is null && postfix is null && transpiler is null && finalizer is null)
			return;

		ref var allPatches = ref CollectionsMarshal.GetValueRefOrAddDefault(this.Patches, original, out var allPatchesExists);
		if (!allPatchesExists)
			allPatches = [];

		ref var patches = ref CollectionsMarshal.GetValueRefOrAddDefault(allPatches!, id, out var patchesExists);
		if (!patchesExists)
			patches = new();

		if (prefix is not null)
			patches.Prefixes.Add(prefix);
		if (postfix is not null)
			patches.Postfixes.Add(postfix);
		if (transpiler is not null)
			patches.Transpilers.Add(transpiler);
		if (finalizer is not null)
			patches.Finalizers.Add(finalizer);
	}

	public void ApplyDelayedPatches()
	{
		lock (GetLocker.Value())
		{
			List<Exception> exceptions = [];
			foreach (var (original, allPatches) in this.Patches)
			{
				try
				{
					var patchInfo = GetPatchInfo.Value(original) ?? new();
					
					foreach (var (id, patches) in allPatches)
					{
						if (patches.Prefixes.Count != 0)
							AddPrefixes.Value(patchInfo, id, [.. patches.Prefixes]);
						if (patches.Postfixes.Count != 0)
							AddPostfixes.Value(patchInfo, id, [.. patches.Postfixes]);
						if (patches.Transpilers.Count != 0)
							AddTranspilers.Value(patchInfo, id, [.. patches.Transpilers]);
						if (patches.Finalizers.Count != 0)
							AddFinalizers.Value(patchInfo, id, [.. patches.Finalizers]);
					}

					var replacement = UpdateWrapper.Value(original, patchInfo);
					UpdatePatchInfo.Value(original, replacement, patchInfo);
				}
				catch (Exception e)
				{
					exceptions.Add(new Exception($"Could not patch method {original.FullDescription()}: {e.Message}", e));
				}
			}

			this.Patches.Clear();
			if (exceptions.Count != 0)
				throw new DelayedPatchingException(exceptions);
		}
	}

	private readonly struct DelayedPatches
	{
		public readonly List<HarmonyMethod> Prefixes = [];
		public readonly List<HarmonyMethod> Postfixes = [];
		public readonly List<HarmonyMethod> Transpilers = [];
		public readonly List<HarmonyMethod> Finalizers = [];

		public DelayedPatches()
		{
		}
	}
}

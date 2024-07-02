using HarmonyLib;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using WeakEvent;

namespace Nickel;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class MapBasePatches
{
	internal static WeakEventSource<GetEnemyPoolsEventArgs> OnGetEnemyPools { get; } = new();
	
	private static readonly GetEnemyPoolsEventArgs GetEnemyPoolsEventArgsInstance = new();

	internal static void ApplyLate(Harmony harmony)
		=> harmony.PatchVirtual(
			original: AccessTools.DeclaredMethod(typeof(MapBase), nameof(MapBase.GetEnemyPools))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(MapBase)}.{nameof(MapBase.GetEnemyPools)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetEnemyPools_Postfix)),
			includeBaseMethod: false
		);

	private static void GetEnemyPools_Postfix(MapBase __instance, State __0, MapBase.MapEnemyPool __result)
	{
		var args = GetEnemyPoolsEventArgsInstance;
		args.State = __0;
		args.Map = __instance;
		args.Pool = __result;
		OnGetEnemyPools.Raise(null, args);
	}

	internal sealed class GetEnemyPoolsEventArgs
	{
		public State State { get; internal set; } = null!;
		public MapBase Map { get; internal set; } = null!;
		public MapBase.MapEnemyPool Pool { get; internal set; } = null!;
	}
}

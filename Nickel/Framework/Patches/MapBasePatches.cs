using HarmonyLib;
using System;
using System.Reflection;
using WeakEvent;

namespace Nickel;

internal static class MapBasePatches
{
	internal static WeakEventSource<GetEnemyPoolsEventArgs> OnGetEnemyPools { get; } = new();

	internal static void ApplyLate(Harmony harmony)
	{
		harmony.PatchVirtual(
			original: AccessTools.DeclaredMethod(typeof(MapBase), nameof(MapBase.GetEnemyPools))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(MapBase)}.{nameof(MapBase.GetEnemyPools)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetEnemyPools_Postfix)),
			includeBaseMethod: false
		);
	}

	private static void GetEnemyPools_Postfix(MapBase __instance, State __0, MapBase.MapEnemyPool __result)
		=> OnGetEnemyPools.Raise(null, new GetEnemyPoolsEventArgs { State = __0, Map = __instance, Pool = __result });

	internal sealed class GetEnemyPoolsEventArgs
	{
		public required State State { get; init; }
		public required MapBase Map { get; init; }
		public required MapBase.MapEnemyPool Pool { get; init; }
	}
}

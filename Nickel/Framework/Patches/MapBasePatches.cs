using HarmonyLib;
using System;
using System.Reflection;

namespace Nickel;

internal static class MapBasePatches
{
	internal static EventHandler<GetEnemyPoolsEventArgs>? OnGetEnemyPools;
	
	private static readonly Pool<GetEnemyPoolsEventArgs> GetEnemyPoolsEventArgsPool = new(() => new());

	internal static void ApplyLate(Harmony harmony)
		=> harmony.PatchVirtual(
			original: AccessTools.DeclaredMethod(typeof(MapBase), nameof(MapBase.GetEnemyPools))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(MapBase)}.{nameof(MapBase.GetEnemyPools)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetEnemyPools_Postfix)),
			includeBaseMethod: false
		);

	private static void GetEnemyPools_Postfix(MapBase __instance, State __0, MapBase.MapEnemyPool __result)
		=> GetEnemyPoolsEventArgsPool.Do(args =>
		{
			args.State = __0;
			args.Map = __instance;
			args.Pool = __result;
			OnGetEnemyPools?.Invoke(null, args);
		});

	internal sealed class GetEnemyPoolsEventArgs
	{
		public State State { get; internal set; } = null!;
		public MapBase Map { get; internal set; } = null!;
		public MapBase.MapEnemyPool Pool { get; internal set; } = null!;
	}
}

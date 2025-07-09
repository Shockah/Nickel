using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nickel;

internal static class MGPatches
{
	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(MG), nameof(MG.MakeLoadingQueue))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(MG)}.{nameof(MG.MakeLoadingQueue)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(MakeLoadingQueue_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(MG), nameof(MG.Initialize))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(MG)}.{nameof(MG.Initialize)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Initialize_Postfix))
		);
	}

	private static void MakeLoadingQueue_Postfix(ref Queue<(string name, Action action)> __result)
	{
		var newQueue = __result.ToList();

		var prewarmingSerializationIndex = newQueue.FindIndex(e => e.name == "prewarming serialization");
		if (prewarmingSerializationIndex != -1)
			newQueue.RemoveAt(prewarmingSerializationIndex);

		var loadSavegameIndex = newQueue.FindIndex(e => e.name == "load savegame, settings");
		if (loadSavegameIndex == -1)
			throw new InvalidOperationException("Could not inject mod loading into game loading queue");

		newQueue.InsertRange(loadSavegameIndex, Nickel.Instance.ModManager.GetGameLoadQueueStepForModLoadPhase(ModLoadPhase.AfterDbInit));
		__result = new Queue<(string name, Action action)>(newQueue);
	}

	private static void Initialize_Postfix(MG __instance)
		=> __instance.Window.Title = NickelConstants.Name;
}

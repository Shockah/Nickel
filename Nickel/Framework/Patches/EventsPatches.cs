using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using WeakEvent;

namespace Nickel;

internal static class EventsPatches
{
	internal static WeakEventSource<List<Choice>> OnCrystallizedFriendEvent { get; } = new();

	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Events), nameof(Events.CrystallizedFriendEvent))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Events)}.{nameof(Events.CrystallizedFriendEvent)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CrystallizedFriendEvent_Postfix))
		);
	}

	private static void CrystallizedFriendEvent_Postfix(ref List<Choice> __result)
		=> OnCrystallizedFriendEvent.Raise(null, __result);
}

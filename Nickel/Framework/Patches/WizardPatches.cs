using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nickel;

internal static class WizardPatches
{
	internal static RefEventHandler<GetAssignableStatusesEventArgs>? OnGetAssignableStatuses;

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Wizard), nameof(Wizard.GetAssignableStatuses))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Wizard)}.{nameof(Wizard.GetAssignableStatuses)}`"),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetAssignableStatuses_Postfix)), priority: Priority.Last)
		);

	private static void GetAssignableStatuses_Postfix(State s, ref List<Status> __result)
	{
		var args = new GetAssignableStatusesEventArgs
		{
			State = s,
			Statuses = __result,
		};
		OnGetAssignableStatuses?.Invoke(null, ref args);
		__result = args.Statuses;
	}

	internal struct GetAssignableStatusesEventArgs
	{
		public required State State { get; init; }
		public required List<Status> Statuses;
	}
}

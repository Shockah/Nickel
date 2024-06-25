using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using WeakEvent;

namespace Nickel;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class WizardPatches
{
	internal static WeakEventSource<GetAssignableStatusesEventArgs> OnGetAssignableStatuses { get; } = new();

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Wizard), nameof(Wizard.GetAssignableStatuses))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Wizard)}.{nameof(Wizard.GetAssignableStatuses)}`"),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetAssignableStatuses_Postfix)), priority: Priority.Last)
		);

	private static void GetAssignableStatuses_Postfix(State s, ref List<Status> __result)
	{
		var eventArgs = new GetAssignableStatusesEventArgs { State = s, Statuses = __result };
		OnGetAssignableStatuses.Raise(null, eventArgs);
		__result = eventArgs.Statuses;
	}

	internal sealed class GetAssignableStatusesEventArgs
	{
		public required State State { get; init; }
		public required List<Status> Statuses { get; set; }
	}
}

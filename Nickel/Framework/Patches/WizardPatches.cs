using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nickel;

internal static class WizardPatches
{
	internal static EventHandler<GetAssignableStatusesEventArgs>? OnGetAssignableStatuses;
		
	private static readonly Pool<GetAssignableStatusesEventArgs> GetAssignableStatusesEventArgsPool = new(() => new());

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Wizard), nameof(Wizard.GetAssignableStatuses))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Wizard)}.{nameof(Wizard.GetAssignableStatuses)}`"),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetAssignableStatuses_Postfix)), priority: Priority.Last)
		);

	private static void GetAssignableStatuses_Postfix(State s, ref List<Status> __result)
	{
		var result = __result;
		GetAssignableStatusesEventArgsPool.Do(args =>
		{
			args.State = s;
			args.Statuses = result;
			OnGetAssignableStatuses?.Invoke(null, args);
			result = args.Statuses;
		});
		__result = result;
	}

	internal sealed class GetAssignableStatusesEventArgs
	{
		public State State { get; internal set; } = null!;
		public List<Status> Statuses { get; set; } = null!;
	}
}

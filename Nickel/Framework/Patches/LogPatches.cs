using HarmonyLib;
using System;
using System.Reflection;

namespace Nickel;

internal static class LogPatches
{
	internal static EventHandler<object>? OnLine;

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Log), nameof(Log.Line))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Log)}.{nameof(Log.Line)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Line_Postfix))
		);

	private static void Line_Postfix(object obj)
		=> OnLine?.Invoke(null, obj);
}

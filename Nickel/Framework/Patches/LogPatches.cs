using HarmonyLib;
using System;
using WeakEvent;

namespace Nickel;

internal static class LogPatches
{
	internal static WeakEventSource<object> OnLine { get; } = new();

	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Log), nameof(Log.Line))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Log)}.{nameof(Log.Line)}`"),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(LogPatches), nameof(Line_Postfix)))
		);
	}

	private static void Line_Postfix(object obj)
		=> OnLine.Raise(null, obj);
}

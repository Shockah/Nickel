using HarmonyLib;
using System;
using System.Collections.Generic;
using WeakEvent;

namespace Nickel;

internal static class DBPatches
{
	internal static WeakEventSource<LoadStringsForLocaleEventArgs> OnLoadStringsForLocale { get; } = new();

	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(DB), nameof(DB.LoadStringsForLocale))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(DB)}.{nameof(DB.LoadStringsForLocale)}`"),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(DBPatches), nameof(LoadStringsForLocale_Postfix)), priority: Priority.Last)
		);
	}

	private static void LoadStringsForLocale_Postfix(string locale, ref Dictionary<string, string>? __result)
	{
		__result ??= [];
		OnLoadStringsForLocale.Raise(null, new LoadStringsForLocaleEventArgs { Locale = locale, Localizations = __result });
	}
}

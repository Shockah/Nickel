using System;
using System.Collections.Generic;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using WeakEvent;

namespace Nickel;

internal static class DBPatches
{
	internal static WeakEventSource<LoadStringsForLocaleEventArgs> OnLoadStringsForLocale { get; } = new();

	internal static void Apply(Harmony harmony, ILogger logger)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(DB), nameof(DB.LoadStringsForLocale)) ?? throw new InvalidOperationException("Could not patch game methods: missing method `DB.LoadStringsForLocale`"),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(DBPatches), nameof(LoadStringsForLocale_Postfix)), priority: Priority.Last)
		);
	}

	private static void LoadStringsForLocale_Postfix(string locale, ref Dictionary<string, string>? __result)
	{
		__result ??= new();
		OnLoadStringsForLocale.Raise(null, new LoadStringsForLocaleEventArgs { Locale = locale, Localizations = __result });
	}
}

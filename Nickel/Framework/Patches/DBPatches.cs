using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using WeakEvent;

namespace Nickel;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class DBPatches
{
	internal static WeakEventSource<LoadStringsForLocaleEventArgs> OnLoadStringsForLocale { get; } = new();

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(DB), nameof(DB.LoadStringsForLocale))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(DB)}.{nameof(DB.LoadStringsForLocale)}`"),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(LoadStringsForLocale_Postfix)), priority: Priority.Last)
		);

	private static void LoadStringsForLocale_Postfix(string locale, ref Dictionary<string, string>? __result)
	{
		__result ??= [];
		OnLoadStringsForLocale.Raise(null, new LoadStringsForLocaleEventArgs { Locale = locale, Localizations = __result });
	}
}

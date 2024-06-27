using HarmonyLib;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Nickel;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class GPatches
{
	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(G), nameof(G.LoadSavegameOnStartup))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(G)}.{nameof(G.LoadSavegameOnStartup)}`"),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(LoadSavegameOnStartup_Prefix)), priority: Priority.First)
		);

	private static void LoadSavegameOnStartup_Prefix(G __instance)
	{
		// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
		if (DB.currentLocale is null)
			DB.SetLocale(__instance.settings.locale, __instance.settings.highResFont);
		
		Nickel.Instance.ModManager.LoadMods(ModLoadPhase.AfterDbInit);
	}
}

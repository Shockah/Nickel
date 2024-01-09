using HarmonyLib;
using System;

namespace Nickel;

internal static class GPatches
{
	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(G), nameof(G.LoadSavegameOnStartup))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(G)}.{nameof(G.LoadSavegameOnStartup)}`"),
			prefix: new HarmonyMethod(typeof(GPatches), nameof(LoadSavegameOnStartup_Prefix))
		);
	}

	private static void LoadSavegameOnStartup_Prefix()
		=> Nickel.Instance.ModManager.LoadMods(ModLoadPhase.AfterDbInit);
}

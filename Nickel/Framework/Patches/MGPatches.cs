using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace Nickel;

internal static class MGPatches
{
    internal static void Apply(Harmony harmony, ILogger logger)
    {
        void PatchInitSync()
        {
            var originalMethod = AccessTools.DeclaredMethod(typeof(MG), "DrawLoadingScreen");
            if (originalMethod is null)
            {
                logger.LogError("Could not patch game methods: missing method.");
                return;
            }

            harmony.Patch(
                original: originalMethod,
                prefix: new HarmonyMethod(typeof(MGPatches), nameof(MG_DrawLoadingScreen_Prefix)),
                postfix: new HarmonyMethod(typeof(MGPatches), nameof(MG_DrawLoadingScreen_Postfix))
            );
        }

        PatchInitSync();
    }

    private static void MG_DrawLoadingScreen_Prefix(MG __instance, ref int __state)
        => __state = __instance.loadingQueue?.Count ?? 0;

    private static void MG_DrawLoadingScreen_Postfix(MG __instance, ref int __state)
    {
        if (__state <= 0)
            return;
        if ((__instance.loadingQueue?.Count ?? 0) > 0)
            return;
        Nickel.Instance.ModManager.LoadMods(ModLoadPhase.AfterDbInit);
    }
}

using System.Linq;
using HarmonyLib;
using Microsoft.Extensions.Logging;

namespace Nickel;

internal static class HarmonyPatches
{
	private static PatchInfo? CurrentPatchInfo;

	internal static void Apply(Harmony harmony, ILogger logger)
	{
		void PatchUpdateWrapper()
		{
			var originalMethod = AccessTools.DeclaredMethod(AccessTools.TypeByName("HarmonyLib.PatchFunctions, 0Harmony"), "UpdateWrapper");
			if (originalMethod is null)
			{
				logger.LogError("Could not patch Harmony methods for better debugging capabilities: missing method.");
				return;
			}

			harmony.Patch(
				original: originalMethod,
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(PatchFunctions_UpdateWrapper_Prefix)),
				postfix: new HarmonyMethod(typeof(HarmonyPatches), nameof(PatchFunctions_UpdateWrapper_Postfix))
			);
		}

		void PatchCreateDynamicMethod()
		{
			var originalMethod = AccessTools.DeclaredMethod(AccessTools.TypeByName("HarmonyLib.MethodPatcher, 0Harmony"), "CreateDynamicMethod");
			if (originalMethod is null)
			{
				logger.LogError("Could not patch Harmony methods for better debugging capabilities: missing method.");
				return;
			}

			harmony.Patch(
				original: originalMethod,
				prefix: new HarmonyMethod(typeof(HarmonyPatches), nameof(MethodPatcher_CreateDynamicMethod_Prefix))
			);
		}

		logger.LogInformation("Preparing Harmony for mod usage...");
		PatchUpdateWrapper();
		PatchCreateDynamicMethod();
	}

	private static void PatchFunctions_UpdateWrapper_Prefix(PatchInfo patchInfo)
	{
		CurrentPatchInfo = patchInfo;
	}

	private static void PatchFunctions_UpdateWrapper_Postfix()
	{
		CurrentPatchInfo = null;
	}

	private static void MethodPatcher_CreateDynamicMethod_Prefix(ref string suffix)
	{
		if (CurrentPatchInfo is null)
			return;

		var owners = CurrentPatchInfo.prefixes
			.Concat(CurrentPatchInfo.postfixes)
			.Concat(CurrentPatchInfo.finalizers)
			.Concat(CurrentPatchInfo.transpilers)
			.Select(p => p.owner)
			.Distinct()
			.ToList();

		if (owners.Count == 0)
		{
			suffix = "_Unpatched";
			return;
		}

		suffix = $"_Patch_{string.Join("_", owners.Select(o => $"<{o}>"))}";
	}
}

using HarmonyLib;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Reflection;

namespace Nickel;

internal static class HarmonyPatches
{
	private static PatchInfo? CurrentPatchInfo;

	internal static void Apply(Harmony harmony, ILogger logger)
	{
		logger.LogDebug("Preparing Harmony for mod usage...");
		PatchUpdateWrapper();
		PatchCreateDynamicMethod();

		void PatchUpdateWrapper()
		{
			if (AccessTools.DeclaredMethod(typeof(Harmony).Assembly.GetType("HarmonyLib.PatchFunctions"), "UpdateWrapper") is not { } originalMethod)
			{
				logger.LogError("Could not patch Harmony methods for better debugging capabilities: missing method.");
				return;
			}

			harmony.Patch(
				original: originalMethod,
				prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(PatchFunctions_UpdateWrapper_Prefix)),
				postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(PatchFunctions_UpdateWrapper_Postfix))
			);
		}

		void PatchCreateDynamicMethod()
		{
			if (AccessTools.DeclaredMethod(typeof(Harmony).Assembly.GetType("HarmonyLib.MethodPatcherTools"), "CreateDynamicMethod") is not { } originalMethod)
			{
				logger.LogError("Could not patch Harmony methods for better debugging capabilities: missing method.");
				return;
			}

			harmony.Patch(
				original: originalMethod,
				prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(MethodPatcherTools_CreateDynamicMethod_Prefix))
			);
		}
	}

	private static void PatchFunctions_UpdateWrapper_Prefix(PatchInfo patchInfo)
		=> CurrentPatchInfo = patchInfo;

	private static void PatchFunctions_UpdateWrapper_Postfix()
		=> CurrentPatchInfo = null;

	private static void MethodPatcherTools_CreateDynamicMethod_Prefix(ref string suffix)
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

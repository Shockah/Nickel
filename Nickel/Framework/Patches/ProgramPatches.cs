using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel;

internal static class ProgramPatches
{
	internal static EventHandler<StructRef<bool>>? OnTryInitSteam;

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Program), nameof(Program.TryInitSteam))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Program)}.{nameof(Program.TryInitSteam)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(TryInitSteam_Transpiler))
		);

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> TryInitSteam_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Call("get_InitSteam"))
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(ProgramPatches), nameof(TryInitSteam_Transpiler_ModifyInitSteam)))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			Nickel.Instance.ModManager.Logger.LogCritical("Could not patch method {Method} - {ModLoaderName} probably won't work.\nReason: {Exception}", originalMethod, NickelConstants.Name, ex);
			return instructions;
		}
	}

	private static bool TryInitSteam_Transpiler_ModifyInitSteam(bool initSteam)
	{
		var args = new StructRef<bool>(initSteam);
		OnTryInitSteam?.Invoke(null, args);
		return args.Value;
	}
}

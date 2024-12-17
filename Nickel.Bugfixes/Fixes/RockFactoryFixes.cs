using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Bugfixes;

internal static class RockFactoryFixes
{
	public static void ApplyPatches(IHarmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.OnBeginTurn))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Ship)}.{nameof(Ship.OnBeginTurn)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_OnBeginTurn_Transpiler))
		);

	private static IEnumerable<CodeInstruction> Ship_OnBeginTurn_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(2),
					ILMatches.Newobj(AccessTools.DeclaredConstructor(typeof(ASpawn))),
					ILMatches.Instruction(OpCodes.Dup),
					ILMatches.Newobj(AccessTools.DeclaredConstructor(typeof(Asteroid))),
					ILMatches.Instruction(OpCodes.Dup),
				])
				.Find(ILMatches.Call("QueueImmediate"))
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_OnBeginTurn_Transpiler_ModifyAction)))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static ASpawn Ship_OnBeginTurn_Transpiler_ModifyAction(ASpawn action, Ship ship)
	{
		action.fromPlayer = ship.isPlayerShip;
		return action;
	}
}

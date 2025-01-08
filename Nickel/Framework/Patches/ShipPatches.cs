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

internal static class ShipPatches
{
	internal static RefEventHandler<ShouldStatusFlashEventArgs>? OnShouldStatusFlash;

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.RenderStatusRow))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Ship)}.{nameof(Ship.RenderStatusRow)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RenderStatusRow_Transpiler))
		);

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> RenderStatusRow_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<Status>(originalMethod).CreateLdlocInstruction(out var ldlocStatus),
					ILMatches.LdcI4((int)Enum.Parse<Status>(nameof(Status.autododgeLeft))),
					ILMatches.Beq,
					ILMatches.Ldloc<Status>(originalMethod),
					ILMatches.LdcI4((int)Enum.Parse<Status>(nameof(Status.autododgeRight))),
					ILMatches.Beq.GetBranchTarget(out var branchTarget)
				])
				.PointerMatcher(branchTarget)
				.Find(ILMatches.Stloc<bool>(originalMethod).CreateLdlocaInstruction(out var ldlocaShouldFlash))
				.Find(ILMatches.Br.GetBranchTarget(out branchTarget))
				.PointerMatcher(branchTarget)
				.ExtractLabels(out var labels)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_1),
					ldlocStatus,
					ldlocaShouldFlash,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RenderStatusRow_Transpiler_ModifyShouldFlash)))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			Nickel.Instance.ModManager.Logger.LogCritical("Could not patch method {Method} - {ModLoaderName} probably won't work.\nReason: {Exception}", originalMethod, NickelConstants.Name, ex);
			return instructions;
		}
	}

	private static void RenderStatusRow_Transpiler_ModifyShouldFlash(Ship ship, G g, Status status, ref bool shouldFlash)
	{
		if (g.state.route is not Combat combat)
			return;
		
		var args = new ShouldStatusFlashEventArgs
		{
			State = g.state,
			Combat = combat,
			Ship = ship,
			Status = status,
			ShouldFlash = shouldFlash
		};
		OnShouldStatusFlash?.Invoke(null, ref args);
		shouldFlash = args.ShouldFlash;
	}

	internal struct ShouldStatusFlashEventArgs
	{
		public required State State { get; init; }
		public required Combat Combat { get; init; }
		public required Ship Ship { get; init; }
		public required Status Status { get; init; }
		public required bool ShouldFlash;
	}
}

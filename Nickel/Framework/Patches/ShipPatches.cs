using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using WeakEvent;

namespace Nickel;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class ShipPatches
{
	internal static WeakEventSource<ShouldStatusFlashEventArgs> OnShouldStatusFlash { get; } = new();
	
	private static readonly ShouldStatusFlashEventArgs ShouldStatusFlashEventArgsInstance = new();

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Ship), nameof(Ship.RenderStatusRow))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Ship)}.{nameof(Ship.RenderStatusRow)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_RenderStatusRow_Transpiler))
		);

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Ship_RenderStatusRow_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
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
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Ship_RenderStatusRow_Transpiler_ModifyShouldFlash)))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			Nickel.Instance.ModManager.Logger.LogCritical("Could not patch method {Method} - {ModLoaderName} probably won't work.\nReason: {Exception}", originalMethod, NickelConstants.Name, ex);
			return instructions;
		}
	}

	private static void Ship_RenderStatusRow_Transpiler_ModifyShouldFlash(Ship ship, G g, Status status, ref bool shouldFlash)
	{
		if (g.state.route is not Combat combat)
			return;
		
		var args = ShouldStatusFlashEventArgsInstance;
		args.State = g.state;
		args.Combat = combat;
		args.Ship = ship;
		args.Status = status;
		args.ShouldFlash = shouldFlash;
		OnShouldStatusFlash.Raise(null, args);
		shouldFlash = args.ShouldFlash;
	}

	internal sealed class ShouldStatusFlashEventArgs
	{
		public State State { get; internal set; } = null!;
		public Combat Combat { get; internal set; } = null!;
		public Ship Ship { get; internal set; } = null!;
		public Status Status { get; internal set; }
		public bool ShouldFlash { get; set; }
	}
}

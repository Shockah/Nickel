using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Bugfixes;

internal static class DisabledActionSpriteFixes
{
	public static void ApplyPatches(IHarmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Card)}.{nameof(Card.RenderAction)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Transpiler))
		);

	public static IEnumerable<CodeInstruction> Card_RenderAction_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Stfld("spriteColor").Element(out var stSpriteColor))
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.WholeSequence, [
					ILMatches.Ldloc(0),
					ILMatches.Ldfld("action"),
					ILMatches.Ldloc(0),
					ILMatches.Ldfld("state"),
					ILMatches.Call("GetIcon"),
					ILMatches.Stloc<Icon?>(originalMethod).CreateLdlocaInstruction(out var ldLoca),
				])
				.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.WholeSequence, [
					ILMatches.Ldloc(0),
					ILMatches.Ldfld("action"),
					ILMatches.Isinst<AAttack>(),
					ILMatches.Stloc<AAttack>(originalMethod).CreateLdlocInstruction(out var ldLoc),
					ILMatches.Ldloc<AAttack>(originalMethod),
					ILMatches.Brfalse,
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldloca, 0),
					new CodeInstruction(OpCodes.Ldloca, 0),
					new CodeInstruction(OpCodes.Ldfld, stSpriteColor.Value.operand),
					new CodeInstruction(OpCodes.Ldloca, ldLoca.Value.operand),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Icon?), nameof(Nullable<Icon>.GetValueOrDefault), [])),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(Icon), nameof(Icon.color))),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(DisabledActionSpriteFixes), nameof(Card_RenderAction_Transpiler_GetFailColorIfFail))),
					new CodeInstruction(OpCodes.Stfld, stSpriteColor.Value.operand),
				])
				.EncompassUntil(SequenceMatcherPastBoundsDirection.After, [
					ILMatches.Ldloc(0),
					ILMatches.Ldfld("w"),
					ILMatches.Instruction(OpCodes.Ret),
				])
				.ForEach(
					SequenceMatcherRelativeBounds.Enclosed,
					[
						ILMatches.Ldsfld("redd"),
						ILMatches.Instruction(OpCodes.Newobj),
						ILMatches.LdcI4(0),
						ILMatches.Ldloca<int?>(originalMethod),
						ILMatches.Instruction(OpCodes.Initobj),
					],
					code => code
						.PointerMatcher(SequenceMatcherRelativeElement.First)
						.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
							new CodeInstruction(OpCodes.Ldloc, ldLoc.Value.operand),
							new CodeInstruction(OpCodes.Ldloca, ldLoca.Value.operand),
							new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Icon?), nameof(Nullable<Icon>.GetValueOrDefault), [])),
							new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(Icon), nameof(Icon.color))),
							new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(DisabledActionSpriteFixes), nameof(Card_RenderAction_Transpiler_GetProperColor)))
						])
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static Color Card_RenderAction_Transpiler_GetFailColorIfFail(Color spriteColor, Color iconColor)
		=> iconColor.ToInt() == Colors.attackFail.ToInt() ? iconColor : spriteColor;

	private static Color Card_RenderAction_Transpiler_GetProperColor(Color _ /* originalColor */, CardAction action, Color iconColor)
		=> action.disabled ? Colors.disabledText : iconColor;
}

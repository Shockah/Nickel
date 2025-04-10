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
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Card)}.{nameof(Card.RenderAction)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Transpiler))
		);
	}

    public static IEnumerable<CodeInstruction> Card_RenderAction_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il, MethodBase originalMethod)
    {
		var stSpriteColor = new SequenceBlockMatcher<CodeInstruction>(instructions).Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.WholeSequence,
			ILMatches.Stfld("spriteColor")
		).Element();

		return new SequenceBlockMatcher<CodeInstruction>(instructions)
		.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.WholeSequence,
			ILMatches.Ldloc(0),
			ILMatches.Ldfld("action"),
			ILMatches.Ldloc(0),
			ILMatches.Ldfld("state"),
			ILMatches.Call("GetIcon"),
			ILMatches.Stloc<Icon?>(originalMethod).CreateLdlocaInstruction(out var ldLoca)
		)
		.Find(SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.WholeSequence,
            ILMatches.Ldloc(0),
			ILMatches.Ldfld("action"),
			ILMatches.Isinst<AAttack>(),
			ILMatches.Stloc<AAttack>(originalMethod).CreateLdlocInstruction(out var ldLoc),
            ILMatches.Ldloc<AAttack>(originalMethod),
            ILMatches.Brfalse
        )
		.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
			new CodeInstruction(OpCodes.Ldloca, 0),
			new(OpCodes.Ldloca, 0),
			new(OpCodes.Ldfld, stSpriteColor.operand),
			new(OpCodes.Ldloca, ldLoca.Value.operand),
			new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Nullable<Icon>), nameof(Nullable<Icon>.GetValueOrDefault), [])),
			new(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(Icon), nameof(Icon.color))),
			new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(DisabledActionSpriteFixes), nameof(GetFailColorIfFail))),
			new(OpCodes.Stfld, stSpriteColor.operand)
		])
		.EncompassUntil(SequenceMatcherPastBoundsDirection.After, [
			ILMatches.Ldloc(0),
			ILMatches.Ldfld("w"),
			ILMatches.Instruction(OpCodes.Ret)
		])
		.ForEach(SequenceMatcherRelativeBounds.Enclosed, [
			ILMatches.Ldsfld("redd"),
			ILMatches.Instruction(OpCodes.Newobj),
			ILMatches.LdcI4(0),
			ILMatches.Ldloca<int?>(originalMethod),
			ILMatches.Instruction(OpCodes.Initobj)
		], code => code.PointerMatcher(SequenceMatcherRelativeElement.First).Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
			new CodeInstruction(OpCodes.Ldloc, ldLoc.Value.operand),
			new CodeInstruction(OpCodes.Ldloca, ldLoca.Value.operand),
			new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Nullable<Icon>), nameof(Nullable<Icon>.GetValueOrDefault), [])),
			new(OpCodes.Ldfld, AccessTools.DeclaredField(typeof(Icon), nameof(Icon.color))),
			// new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(Color), "op_Multiply"))
			new(OpCodes.Call, AccessTools.DeclaredMethod(typeof(DisabledActionSpriteFixes), nameof(GetProperColor)))
		]))
		.AllElements();
    }

	private static Color GetFailColorIfFail(Color spriteColor, Color iconColor) {
		if (iconColor.ToInt() == Colors.attackFail.ToInt()) return iconColor;
		return spriteColor;
	}

	private static Color GetProperColor(Color originalColor, CardAction action, Color iconColor) {
		return action.disabled ? Colors.disabledText : iconColor;
	}
}
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Microsoft.Extensions.Logging;

namespace Nickel.Bugfixes;

internal static class SetStatusXFixes
{
	public static void ApplyPatches(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(AStatus), nameof(AStatus.GetIcon))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(AStatus)}.{nameof(AStatus.GetIcon)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(AStatus_GetIcon_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.RenderAction))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Card)}.{nameof(Card.RenderAction)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Transpiler))
		);
	}

	private static void AStatus_GetIcon_Postfix(AStatus __instance, ref Icon? __result)
	{
		if (__instance.mode == AStatusMode.Set && __result is { number: null } icon)
			__result = new Icon(icon.path, __instance.statusAmount, icon.color, icon.flipY);
	}

	private static IEnumerable<CodeInstruction> Card_RenderAction_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.AnyLdloca.GetLocalIndex(out var capturesLocalIndex),
					ILMatches.AnyLdloc,
					ILMatches.Ldfld("w").SelectElement(out var capturedWField, i => (FieldInfo)i.operand),
					ILMatches.LdcI4(3),
					ILMatches.Instruction(OpCodes.Add),
					ILMatches.Stfld("w"),
				])
				.Find([
					ILMatches.Ldfld(AccessTools.DeclaredField(typeof(Icon), nameof(Icon.flipY))),
					ILMatches.Ldloc(capturesLocalIndex),
					ILMatches.Ldfld("action").SelectElement(out var capturedActionField, i => (FieldInfo)i.operand),
					ILMatches.Ldfld(AccessTools.DeclaredField(typeof(CardAction), nameof(CardAction.xHint))),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.JustInsertion, [
					new CodeInstruction(OpCodes.Ldloc, capturesLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldfld, capturedActionField.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Transpiler_PassNullInsteadOfX))),
				])
				.Find(SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.Before, [
					ILMatches.AnyLdloc,
					ILMatches.Ldfld(AccessTools.DeclaredField(typeof(Icon), nameof(Icon.path))),
					ILMatches.AnyLdloc,
					ILMatches.Ldfld(AccessTools.DeclaredField(typeof(Icon), nameof(Icon.number))),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.JustInsertion, [
					new CodeInstruction(OpCodes.Ldloc, capturesLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldfld, capturedActionField.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Transpiler_PassNullInsteadOfX))),
				])
				.Find([
					ILMatches.AnyLdloc.GetLocalIndex(out var statusActionLocalIndex),
					ILMatches.Ldfld(AccessTools.DeclaredField(typeof(AStatus), nameof(AStatus.mode))),
					ILMatches.LdcI4(1),
					ILMatches.BneUn.GetBranchTarget(out var statusModeNotSetBranchTarget),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.JustInsertion, [
					new CodeInstruction(OpCodes.Ldloca, capturesLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldflda, capturedWField.Value),
					new CodeInstruction(OpCodes.Ldarg_0), // g
					new CodeInstruction(OpCodes.Ldarg_1), // state
					new CodeInstruction(OpCodes.Ldarg_3), // dontDraw
					new CodeInstruction(OpCodes.Ldloc, statusActionLocalIndex.Value), // astatus
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Transpiler_RenderTheOtherBitOfSetX))),
					new CodeInstruction(OpCodes.Ldloc, capturesLocalIndex.Value),
					new CodeInstruction(OpCodes.Ldfld, capturedActionField.Value),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_Transpiler_SkipRegularRender))),
					new CodeInstruction(OpCodes.Brtrue, statusModeNotSetBranchTarget.Value),
				]).AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static int? Card_RenderAction_Transpiler_PassNullInsteadOfX(int? hint, CardAction action)
		=> action is AStatus { mode: AStatusMode.Set } ? null : hint;

	private static void Card_RenderAction_Transpiler_RenderTheOtherBitOfSetX(ref int w, G g, State state, bool dontDraw, AStatus action)
	{
		if (action.xHint is not { } xHint)
			return;
		if (action.GetIcon(state) is not { } icon)
			return;
		
		w += 2;
		
		if (!dontDraw)  // = sign
		{
			var box = g.Push(null, new Rect(w));
			var textColor = action.disabled ? Colors.disabledText : Colors.textMain;
			Draw.Text("=", box.rect.x, box.rect.y + 2, color: textColor, outline: Colors.black, dontSubstituteLocFont: true);
			g.Pop();
		}
		w += 5;

		if (xHint < 0)
		{
			if (!dontDraw)  // - sign
			{
				var box = g.Push(null, new Rect(w));
				var iconColor = action.disabled ? Colors.disabledIconTint : icon.color;
				Draw.Sprite(StableSpr.icons_minus, box.rect.x - 2, box.rect.y - 1, color: iconColor);
				g.Pop();
			}
			w += 5;
		}

		if (Math.Abs(xHint) > 1)
		{
			w += 1;
			if (!dontDraw)
			{
				var box = g.Push(null, new Rect(w));
				var textColor = action.disabled ? Colors.disabledText : Colors.textMain;
				BigNumbers.Render(Math.Abs(xHint), box.rect.x, box.rect.y, textColor);
				g.Pop();
			}
			w += $"{Math.Abs(xHint)}".Length * 6;
		}

		if (!dontDraw)
		{
			var box = g.Push(null, new Rect(w));
			var iconColor = action.disabled ? Colors.disabledIconTint : icon.color;
			Draw.Sprite(StableSpr.icons_x_white, box.rect.x, box.rect.y - 1, color: iconColor);
			g.Pop();
		}

		w += 8;
	}

	private static bool Card_RenderAction_Transpiler_SkipRegularRender(CardAction action)
		=> action.xHint is not null;
}

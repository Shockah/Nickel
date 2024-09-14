using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Essentials;

internal static class CardActionRenderingFixes
{
	public static void ApplyPatches(IHarmony harmony)
	{
		harmony.Patch(
			original: typeof(Card).GetMethods(AccessTools.all).First(m => m.Name.StartsWith("<RenderAction>g__IconAndOrNumber") && m.ReturnType == typeof(void))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Card)}.{nameof(Card.RenderAction)}.IconAndOrNumber`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_IconAndOrNumber_Transpiler))
		);
		harmony.Patch(
			original: typeof(Card).GetMethods(AccessTools.all).First(m => m.Name.StartsWith("<RenderAction>g__ParenIconParen") && m.ReturnType == typeof(void))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Card)}.{nameof(Card.RenderAction)}.ParenIconParen`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_ParenIconParen_Transpiler))
		);
	}
	
	private static IEnumerable<CodeInstruction> Card_RenderAction_IconAndOrNumber_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(SequenceBlockMatcherFindOccurence.Last, SequenceMatcherRelativeBounds.WholeSequence, [
					ILMatches.Call("GetValueOrDefault"),
					ILMatches.Ldfld("color")
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg, 5),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(originalMethod.GetParameters()[5].ParameterType.GetElementType(), "action")),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_IconAndOrNumber_Transpiler_ModifyXColor)))
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

	private static Color Card_RenderAction_IconAndOrNumber_Transpiler_ModifyXColor(Color currentColor, CardAction action)
	{
		if (!action.disabled)
			return currentColor;
		var fadeColor = new Color(Colors.disabledText.r / Colors.textMain.r, Colors.disabledText.g / Colors.textMain.g, Colors.disabledText.b / Colors.textMain.b, Colors.disabledText.a / Colors.textMain.a);
		return currentColor * fadeColor;
	}
	
	private static IEnumerable<CodeInstruction> Card_RenderAction_ParenIconParen_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Call("GetValueOrDefault"),
					ILMatches.Ldfld("color")
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldfld, AccessTools.DeclaredField(originalMethod.GetParameters()[0].ParameterType.GetElementType(), "action")),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_RenderAction_ParenIconParen_Transpiler_ModifyXColor)))
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

	private static Color Card_RenderAction_ParenIconParen_Transpiler_ModifyXColor(Color currentColor, CardAction action)
	{
		if (!action.disabled)
			return currentColor;
		var fadeColor = new Color(Colors.disabledText.r / Colors.textMain.r, Colors.disabledText.g / Colors.textMain.g, Colors.disabledText.b / Colors.textMain.b, Colors.disabledText.a / Colors.textMain.a);
		return currentColor * fadeColor;
	}
}

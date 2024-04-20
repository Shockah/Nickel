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

internal static class MemorySelection
{
	private const int MaxCharactersOnScreen = 6;

	private static int ScrollPosition = 0;

	private static int MaxScroll
	{
		get
		{
			var maxScroll = Math.Max(0, Vault.charsWithLore.Count - MaxCharactersOnScreen);
			return maxScroll;
		}
	}

	public static void ApplyPatches(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Route), nameof(Route.OnEnter)),
			postfix: new HarmonyMethod(typeof(MemorySelection), nameof(Route_OnEnter_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Vault), nameof(Vault.Render)),
			prefix: new HarmonyMethod(typeof(MemorySelection), nameof(Vault_Render_Prefix)),
			postfix: new HarmonyMethod(typeof(MemorySelection), nameof(Vault_Render_Postfix)),
			transpiler: new HarmonyMethod(typeof(MemorySelection), nameof(Vault_Render_Transpiler))
		);
	}

	private static void Route_OnEnter_Postfix(Route __instance)
	{
		// reset the scroll position to the very left
		if (__instance is Vault)
			ScrollPosition = 0;
	}

	private static void Vault_Render_Prefix(Vault __instance)
	{
		if (__instance.subRoute is not null)
			return;

		// handling mouse scroll wheel to page the character list
		var mouseScroll = (int)Math.Round(-Input.scrollY / 120);
		if (mouseScroll != 0)
			ScrollPosition = Math.Clamp(ScrollPosition + mouseScroll, 0, MaxScroll);
	}

	private static void Vault_Render_Postfix(Vault __instance, G g)
	{
		if (__instance.subRoute is not null)
			return;

		// rendering character list scrolling arrow buttons
		if (ScrollPosition > 0)
		{
			Rect rect = new(8, 114, 24, 33);
			OnMouseDown onMouseDown = new MouseDownHandler(() => ScrollPosition = Math.Max(0, ScrollPosition - MaxCharactersOnScreen));
			SharedArt.ButtonSprite(g, rect, StableUK.btn_move_left, StableSpr.buttons_move, StableSpr.buttons_move_on, flipX: true, onMouseDown: onMouseDown);
		}
		if (ScrollPosition < MaxScroll)
		{
			Rect rect = new(446, 114, 24, 33);
			OnMouseDown onMouseDown = new MouseDownHandler(() => ScrollPosition = Math.Clamp(ScrollPosition + MaxCharactersOnScreen, 0, MaxScroll));
			SharedArt.ButtonSprite(g, rect, StableUK.btn_move_right, StableSpr.buttons_move, StableSpr.buttons_move_on, flipX: false, onMouseDown: onMouseDown);
		}
	}

	private static IEnumerable<CodeInstruction> Vault_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				// limit finale unlock to only 6 vanilla chars
				.Find(
					ILMatches.Ldloc<List<Vault.MemorySet>>(originalMethod),
					ILMatches.Instruction(OpCodes.Ldsfld).Anchor(out var delegateAnchor),
					ILMatches.Instruction(OpCodes.Dup),
					ILMatches.Brtrue
				)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldc_I4, MaxCharactersOnScreen),
					new CodeInstruction(OpCodes.Call, typeof(Enumerable).GetMethods().First(m => m.Name == "Take" && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType == typeof(int)).MakeGenericMethod(typeof(Vault.MemorySet)))
				)
				// change for loop initial value to scrolled one
				.Find(
					SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.After,
					ILMatches.LdcI4(0),
					ILMatches.Stloc<int>(originalMethod),
					ILMatches.Br
				)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.Replace(new CodeInstruction(OpCodes.Ldsfld, AccessTools.DeclaredField(typeof(MemorySelection), nameof(ScrollPosition))))
				// change memory count for x/width to constant
				.Find(
					SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.After,
					ILMatches.Ldloc<List<Vault.MemorySet>>(originalMethod),
					ILMatches.Call("get_Count")
				)
				.Replace(new CodeInstruction(OpCodes.Ldc_I4, MaxCharactersOnScreen))
				// change memory offset to map to new scrolled range
				.Find(
					SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.After,
					ILMatches.Ldloc<int>(originalMethod),
					ILMatches.Instruction(OpCodes.Conv_R8),
					ILMatches.Instruction(OpCodes.Add),
					ILMatches.LdcR8(66),
					ILMatches.Instruction(OpCodes.Mul)
				)
				.PointerMatcher(SequenceMatcherRelativeElement.First)
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.DeclaredField(typeof(MemorySelection), nameof(ScrollPosition))),
					new CodeInstruction(OpCodes.Sub)
				)
				// change for loop limit to scrolled one
				.Find(
					SequenceBlockMatcherFindOccurence.First, SequenceMatcherRelativeBounds.After,
					ILMatches.Ldloc<List<Vault.MemorySet>>(originalMethod),
					ILMatches.Call("get_Count")
				)
				.Replace(
					new CodeInstruction(OpCodes.Ldsfld, AccessTools.DeclaredField(typeof(MemorySelection), nameof(ScrollPosition))),
					new CodeInstruction(OpCodes.Ldc_I4, MaxCharactersOnScreen),
					new CodeInstruction(OpCodes.Add)
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}
}

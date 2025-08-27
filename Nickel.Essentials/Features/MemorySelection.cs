using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Essentials;

internal static class MemorySelection
{
	private const int MaxCharactersOnScreen = 6;

	private static int ScrollPosition;

	private static int MaxScroll
	{
		get
		{
			var maxScroll = Math.Max(0, Vault.charsWithLore.Count - MaxCharactersOnScreen);
			return maxScroll;
		}
	}

	public static void ApplyPatches(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Route), nameof(Route.OnEnter)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Route_OnEnter_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Vault), nameof(Vault.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Vault_Render_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Vault_Render_Postfix)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Vault_Render_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(RunWinHelpers), nameof(RunWinHelpers.GetChoices)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RunWinHelpers_GetChoices_Postfix))
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
		var pulseColor = new Color(0.0, 0.5, 1.0).gain((Math.Sin(g.time * Math.PI) + 1) / 4 + 0.25);
		if (ScrollPosition > 0)
		{
			var rect = new Rect(8, 114, 24, 33);
			var onMouseDown = new MouseDownHandler(() => ScrollPosition = Math.Max(0, ScrollPosition - MaxCharactersOnScreen));
			var result = SharedArt.ButtonSprite(g, rect, StableUK.btn_move_left, StableSpr.buttons_move, StableSpr.buttons_move_on, flipX: true, onMouseDown: onMouseDown);
			if (!result.isHover && Vault.GetVaultMemories(g.state).Take(ScrollPosition).Any(m => m.memoryKeys.Any(mk => mk is { unlocked: true, seen: false })))
				Glow.Draw(rect.Center() + new Vec(2, -1), 64, pulseColor);
		}
		if (ScrollPosition < MaxScroll)
		{
			var rect = new Rect(446, 114, 24, 33);
			var onMouseDown = new MouseDownHandler(() => ScrollPosition = Math.Clamp(ScrollPosition + MaxCharactersOnScreen, 0, MaxScroll));
			var result = SharedArt.ButtonSprite(g, rect, StableUK.btn_move_right, StableSpr.buttons_move, StableSpr.buttons_move_on, flipX: false, onMouseDown: onMouseDown);
			if (!result.isHover && Vault.GetVaultMemories(g.state).Skip(ScrollPosition + 6).Any(m => m.memoryKeys.Any(mk => mk is { unlocked: true, seen: false })))
				Glow.Draw(rect.Center() + new Vec(-2, -1), 64, pulseColor);
		}
	}

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Vault_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				// limit finale unlock to only 6 vanilla chars
				.Find(
					ILMatches.Ldloc<List<Vault.MemorySet>>(originalMethod),
					ILMatches.Instruction(OpCodes.Ldsfld),
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
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static void RunWinHelpers_GetChoices_Postfix(State s, ref List<Choice> __result)
	{
		var allMemories = Vault.GetVaultMemories(s);
		
		foreach (var character in s.characters)
		{
			if (character.deckType is not { } deck)
				continue;
			if (HasMemoriesToUnlock(deck))
				continue;

			var index = __result.FindIndex(c => c.key == $".runWin_{deck.Key()}");
			if (index >= 0)
				__result.RemoveAt(index);
		}

		bool HasMemoriesToUnlock(Deck deck)
		{
			if (allMemories.FirstOrDefault(m => m.deck == deck) is not { } memories)
				return false;
			return memories.memoryKeys.Any(m => !m.unlocked);
		}
	}
}

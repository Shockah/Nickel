using HarmonyLib;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Essentials;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class TooltipScrolling
{
	private static string LastTooltipId = "";
	private static Rect LastTooltipRect;
	private static double Scroll;
	private static double ScrollTarget;
	private static double HijackedScrollY;
	private static double LastActivityTime;
	private static string LastHijackedTooltipId = "";

	public static void ApplyPatches(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Tooltip), nameof(Tooltip.RenderMultiple)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Tooltip_RenderMultiple_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Input), nameof(Input.UpdateMouseButtons)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Input_UpdateMouseButtons_Postfix))
		);
	}

	private static string GetTooltipId(List<Tooltip> tooltips)
		=> string.Join(
			"\n",
			tooltips.Select(t => t switch
			{
				TTCard card => $"TTCard::{card.card.Key()}::{card.card.upgrade}",
				TTGlossary glossary => $"TTGlossary::{glossary.key}",
				TTText text => $"TTText::{text.text}",
				_ => t.GetType().FullName
			})
		);

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Tooltip_RenderMultiple_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Call("KeepInside"))
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Tooltip_RenderMultiple_Transpiler_ModifyRect)))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static Rect Tooltip_RenderMultiple_Transpiler_ModifyRect(Rect rect, G g, List<Tooltip> tooltips)
	{
		var oldInputScrollY = Input.scrollY;
		Input.scrollY = HijackedScrollY;

		try
		{
			var tooltipId = GetTooltipId(tooltips);
			if (rect.xy != LastTooltipRect.xy || tooltipId != LastTooltipId)
			{
				Scroll = 0;
				ScrollTarget = 0;
			}
		
			LastTooltipId = tooltipId;
			LastTooltipRect = rect;
		
			if (rect.y2 <= Tooltips.SCREEN_LIMITS.y2)
				return rect;

			var maxScroll = (int)(rect.y2 - Tooltips.SCREEN_LIMITS.y2);
			ScrollUtils.ReadScrollInputAndUpdate(g.dt, maxScroll, ref Scroll, ref ScrollTarget);

			return new(rect.x, (int)(rect.y + Scroll), rect.w, rect.h);
		}
		finally
		{
			Input.scrollY = oldInputScrollY;
		}
	}

	private static void Input_UpdateMouseButtons_Postfix()
	{
		HijackedScrollY = 0;
		
		if (FeatureFlags.Debug && ImGui.GetIO().WantCaptureMouse)
			return;
		if (LastTooltipRect.h <= Tooltips.SCREEN_LIMITS.h)
			return;

		if (MG.inst.g.tooltips.tooltips.Count == 0 || MG.inst.g.tooltips.tooltipTimer < Tooltips.TOOLTIP_DELAY)
		{
			LastTooltipId = "";
			LastHijackedTooltipId = "";
			LastActivityTime = MG.inst.g.time;
			return;
		}

		if (Input.gamepadIsActiveInput)
		{
			var state = GamePad.GetState(PlayerIndex.One);
			HijackedScrollY = state.ThumbSticks.Right.Y * 10;
			return;
		}

		var shouldHijack = MG.inst.g.time - LastActivityTime >= 0.3;
		if (LastActivityTime == 0 || Input.mouseLeftDown || Input.mouseRightDown || LastHijackedTooltipId != LastTooltipId || (!shouldHijack && Input.scrollY != 0))
		{
			LastActivityTime = MG.inst.g.time;
			LastHijackedTooltipId = LastTooltipId;
		}
		shouldHijack = MG.inst.g.time - LastActivityTime >= 0.3;
		
		if (!shouldHijack)
			return;

		HijackedScrollY = Input.scrollY;
		Input.scrollY = 0;
	}
}

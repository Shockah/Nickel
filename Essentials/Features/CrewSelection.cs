using daisyowl.text;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Essentials;

internal static class CrewSelection
{
	private const int CharactersPerRow = 2;
	private const int MaxCharactersOnScreen = 8;

	private static readonly Lazy<Func<Rect>> CharSelectPosGetter = new(() => AccessTools.DeclaredField(typeof(NewRunOptions), "charSelectPos").EmitStaticGetter<Rect>());

	private static int ScrollPosition = 0;

	private static int MaxScroll
	{
		get
		{
			var totalRows = (int)Math.Ceiling((double)NewRunOptions.allChars.Count / CharactersPerRow);
			var maxScroll = Math.Max(0, totalRows * CharactersPerRow - MaxCharactersOnScreen);
			return maxScroll;
		}
	}

	private static int MaxPageByPageScroll
	{
		get
		{
			var totalPages = (int)Math.Ceiling((double)NewRunOptions.allChars.Count / MaxCharactersOnScreen);
			var maxScroll = Math.Max(0, (totalPages - 1) * MaxCharactersOnScreen);
			return maxScroll;
		}
	}

	public static void ApplyPatches(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.OnEnter)),
			postfix: new HarmonyMethod(typeof(CrewSelection), nameof(NewRunOptions_OnEnter_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.Render)),
			prefix: new HarmonyMethod(typeof(CrewSelection), nameof(NewRunOptions_Render_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), "CharSelect"),
			postfix: new HarmonyMethod(typeof(CrewSelection), nameof(NewRunOptions_CharSelect_Postfix)),
			transpiler: new HarmonyMethod(typeof(CrewSelection), nameof(NewRunOptions_CharSelect_Transpiler))
		);
	}

	private static void NewRunOptions_OnEnter_Postfix()
		// reset the scroll position to the very top
		=> ScrollPosition = 0;

	private static void NewRunOptions_Render_Prefix()
	{
		// handling mouse scroll wheel to page the character list
		var mouseScroll = (int)Math.Round(-Input.scrollY / 120);
		if (mouseScroll != 0)
			ScrollPosition = Math.Clamp(ScrollPosition + CharactersPerRow * mouseScroll, 0, MaxScroll);
	}

	private static void NewRunOptions_CharSelect_Postfix(G g)
	{
		// rendering character list scrolling arrow buttons
		var charSelectPos = CharSelectPosGetter.Value();

		if (ScrollPosition > 0)
		{
			Rect rect = new(charSelectPos.x + 18, charSelectPos.y - 52, 33, 24);
			OnMouseDown onMouseDown = new MouseDownHandler(() => ScrollPosition = Math.Max(0, ScrollPosition - MaxCharactersOnScreen));
			RotatedButtonSprite(g, rect, StableUK.btn_move_left, StableSpr.buttons_move, StableSpr.buttons_move_on, null, null, inactive: false, flipX: true, flipY: false, onMouseDown, autoFocus: false, noHover: false, gamepadUntargetable: true);
		}

		if (ScrollPosition < MaxScroll)
		{
			Rect rect = new(charSelectPos.x + 18, charSelectPos.y + 140, 33, 24);
			OnMouseDown onMouseDown = new MouseDownHandler(() => ScrollPosition = Math.Clamp(ScrollPosition + MaxCharactersOnScreen, 0, MaxPageByPageScroll));
			RotatedButtonSprite(g, rect, StableUK.btn_move_right, StableSpr.buttons_move, StableSpr.buttons_move_on, null, null, inactive: false, flipX: false, flipY: false, onMouseDown, autoFocus: false, noHover: false, gamepadUntargetable: true);
		}
	}

	private static IEnumerable<CodeInstruction> NewRunOptions_CharSelect_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// hijacking the vanilla "Crew" and "X of 3" texts
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				// hijack vanilla "Crew" text for all our rendering
				.Find(ILMatches.Ldstr("newRunOptions.crew"))
				.Find(ILMatches.Call("Text"))
				.Replace(
					new CodeInstruction(OpCodes.Ldarg_2),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CrewSelection), nameof(NewRunOptions_CharSelect_Transpiler_HijackDrawCrewText)))
				)

				// hijack vanilla "X of 3" text and stop it from rendering altogether
				.Find(ILMatches.Ldstr("newRunOptions.crewCount"))
				.Find(ILMatches.Call("Text"))
				.Replace(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CrewSelection), nameof(NewRunOptions_CharSelect_Transpiler_HijackDrawCrewCountText))))

				// modify character list to only show the currently scrolled to 8 characters
				.Find(ILMatches.Ldsfld("allChars"))
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CrewSelection), nameof(NewRunOptions_CharSelect_Transpiler_ModifyAllChars)))
				)

				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static Rect NewRunOptions_CharSelect_Transpiler_HijackDrawCrewText(string str, double x, double y, Font? font, Color? color, Color? colorForce, double? progress, double? maxWidth, TAlign? align, bool dontDraw, int? lineHeight, Color? outline, BlendState? blend, SamplerState? samplerState, Effect? effect, bool dontSubstituteLocFont, double letterSpacing, double extraScale, RunConfig runConfig)
	{
		var orderedSelectedChars = runConfig.selectedChars.OrderBy(NewRunOptions.allChars.IndexOf).ToList();
		for (var i = 0; i < 3; i++)
		{
			Deck? deck = i < orderedSelectedChars.Count ? orderedSelectedChars[i] : null;
			var charText = deck is null ? ModEntry.Instance.Localizations.Localize(["crewSelection", "emptySlot"]) : Loc.T($"char.{deck.Value.Key()}");
			var charTextColor = deck is null || !DB.decks.TryGetValue(deck.Value, out var deckDef) ? Colors.downside.fadeAlpha(0.4) : deckDef.color;
			Draw.Text(charText, x, y - 5 + i * 8, font, charTextColor);
		}
		return new();
	}

	[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Hijacking a real method call")]
	private static Rect NewRunOptions_CharSelect_Transpiler_HijackDrawCrewCountText(string str, double x, double y, Font? font, Color? color, Color? colorForce, double? progress, double? maxWidth, TAlign? align, bool dontDraw, int? lineHeight, Color? outline, BlendState? blend, SamplerState? samplerState, Effect? effect, bool dontSubstituteLocFont, double letterSpacing, double extraScale)
		// do nothing
		=> new();

	private static List<Deck> NewRunOptions_CharSelect_Transpiler_ModifyAllChars(List<Deck> allChars)
		=> allChars.Skip(ScrollPosition).Take(MaxCharactersOnScreen).ToList();

	// mostly copy-paste of SharedArt.ButtonResult, without too many improvements
	private static SharedArt.ButtonResult RotatedButtonSprite(G g, Rect rect, UIKey key, Spr sprite, Spr spriteHover, Spr? spriteDown = null, Color? boxColor = null, bool inactive = false, bool flipX = false, bool flipY = false, OnMouseDown? onMouseDown = null, bool autoFocus = false, bool noHover = false, bool showAsPressed = false, bool gamepadUntargetable = false, UIKey? leftHint = null, UIKey? rightHint = null)
	{
		var box = g.Push(key, rect, null, autoFocus, inactive, gamepadUntargetable, ReticleMode.Quad, onMouseDown, null, null, null, 0, rightHint, leftHint);
		var xy = box.rect.xy;
		var isPressed = !noHover && (box.IsHover() || showAsPressed) && !inactive;
		if (spriteDown.HasValue && box.IsHover() && Input.mouseLeft)
			showAsPressed = true;
		var rotation = Math.PI / 2;
		Draw.Sprite((!showAsPressed) ? (isPressed ? spriteHover : sprite) : (spriteDown ?? spriteHover), xy.x + Math.Sin(rotation) * rect.w, xy.y - Math.Cos(rotation) * rect.h, flipX, flipY, rotation, null, null, null, null, boxColor);
		SharedArt.ButtonResult buttonResult = default;
		buttonResult.isHover = isPressed;
		buttonResult.FIXME_isHoverForTooltip = !noHover && box.IsHover();
		buttonResult.v = xy;
		buttonResult.innerOffset = new Vec(0.0, showAsPressed ? 2 : (isPressed ? 1 : 0));
		g.Pop();
		return buttonResult;
	}

	private sealed record MouseDownHandler(Action Delegate) : OnMouseDown
	{
		public void OnMouseDown(G _1, Box _2)
			=> this.Delegate();
	}
}

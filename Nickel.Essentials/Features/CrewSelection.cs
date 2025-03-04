using daisyowl.text;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Microsoft.Xna.Framework.Graphics;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Essentials;

internal sealed partial class ProfileSettings
{
	[JsonProperty]
	public bool DetailedCrewInfo = true;
}

internal static class CrewSelection
{
	private const int CharactersPerRow = 2;
	private const int MaxCharactersOnScreen = 8;

	private static int ScrollPosition;

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

	public static void ApplyPatches(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.OnEnter)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_OnEnter_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_Render_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.CharSelect)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_CharSelect_Postfix)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_CharSelect_Transpiler))
		);
	}

	public static IModSettingsApi.IModSetting MakeSettings(IModSettingsApi api)
		=> api.MakeCheckbox(
			title: () => ModEntry.Instance.Localizations.Localize(["crewSelection", "detailedCrewInfoSetting", "name"]),
			getter: () => ModEntry.Instance.Settings.ProfileBased.Current.DetailedCrewInfo,
			setter: (_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.DetailedCrewInfo = value
		).SetTooltips(() => [
			new GlossaryTooltip($"settings.{ModEntry.Instance.Package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}::DetailedCrewInfoSetting")
			{
				TitleColor = Colors.textBold,
				Title = ModEntry.Instance.Localizations.Localize(["crewSelection", "detailedCrewInfoSetting", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["crewSelection", "detailedCrewInfoSetting", "description"])
			}
		]);

	private static void NewRunOptions_OnEnter_Postfix()
		// reset the scroll position to the very top
		=> ScrollPosition = 0;

	private static void NewRunOptions_Render_Prefix()
	{
		if (ShipSelection.ShowingShips)
			return;
		
		// handling mouse scroll wheel to page the character list
		var mouseScroll = (int)Math.Round(-Input.scrollY / 120);
		if (mouseScroll != 0)
			ScrollPosition = Math.Clamp(ScrollPosition + CharactersPerRow * mouseScroll, 0, MaxScroll);
	}

	private static void NewRunOptions_CharSelect_Postfix(G g, bool __runOriginal)
	{
		if (!__runOriginal)
			return;
		
		// rendering character list scrolling arrow buttons
		if (ScrollPosition > 0)
		{
			Rect rect = new(NewRunOptions.charSelectPos.x + 19, NewRunOptions.charSelectPos.y - 53, 31, 26);
			OnMouseDown onMouseDown = new MouseDownHandler(() => ScrollPosition = Math.Max(0, ScrollPosition - MaxCharactersOnScreen));
			SharedArt.ButtonSprite(g, rect, StableUK.btn_move_left, ModEntry.Instance.ScrollUpSprite.Sprite, ModEntry.Instance.ScrollUpOnSprite.Sprite, onMouseDown: onMouseDown);
		}
		if (ScrollPosition < MaxScroll)
		{
			Rect rect = new(NewRunOptions.charSelectPos.x + 19, NewRunOptions.charSelectPos.y + 140, 31, 26);
			OnMouseDown onMouseDown = new MouseDownHandler(() => ScrollPosition = Math.Clamp(ScrollPosition + MaxCharactersOnScreen, 0, MaxPageByPageScroll));
			SharedArt.ButtonSprite(g, rect, StableUK.btn_move_right, ModEntry.Instance.ScrollDownSprite.Sprite, ModEntry.Instance.ScrollDownOnSprite.Sprite, onMouseDown: onMouseDown);
		}
	}

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
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
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
	}

	private static Rect NewRunOptions_CharSelect_Transpiler_HijackDrawCrewText(string str, double x, double y, Font? font, Color? color, Color? colorForce, double? progress, double? maxWidth, TAlign? align, bool dontDraw, int? lineHeight, Color? outline, BlendState? blend, SamplerState? samplerState, Effect? effect, bool dontSubstituteLocFont, double letterSpacing, double extraScale, RunConfig runConfig)
	{
		if (!ModEntry.Instance.Settings.ProfileBased.Current.DetailedCrewInfo)
			return Draw.Text(str, x, y, font, color, colorForce, progress, maxWidth, align, dontDraw, lineHeight, outline, blend, samplerState, effect, dontSubstituteLocFont, letterSpacing, extraScale);

		var orderedSelectedChars = runConfig.selectedChars.OrderBy(NewRunOptions.allChars.IndexOf).ToList();
		for (var i = 0; i < 3; i++)
		{
			Deck? deck = i < orderedSelectedChars.Count ? orderedSelectedChars[i] : null;
			var altStarters = deck is not null && (ModEntry.Instance.MoreDifficultiesApi?.AreAltStartersEnabled(MG.inst.g.state, deck.Value) ?? false);
			var charText = deck is null ? ModEntry.Instance.Localizations.Localize(["crewSelection", "emptySlot"]) : $"{Loc.T($"char.{deck.Value.Key()}")}{(altStarters ? "*" : "")}";
			var charTextColor = deck is null || !DB.decks.TryGetValue(deck.Value, out var deckDef) ? Colors.downside.fadeAlpha(0.4) : deckDef.color;
			Draw.Text(charText, x, y - 5 + i * 8, font, charTextColor);
		}
		return new();
	}

	private static Rect NewRunOptions_CharSelect_Transpiler_HijackDrawCrewCountText(string str, double x, double y, Font? font, Color? color, Color? colorForce, double? progress, double? maxWidth, TAlign? align, bool dontDraw, int? lineHeight, Color? outline, BlendState? blend, SamplerState? samplerState, Effect? effect, bool dontSubstituteLocFont, double letterSpacing, double extraScale)
	{
		if (ModEntry.Instance.Settings.ProfileBased.Current.DetailedCrewInfo)
			return new();
		return Draw.Text(str, x, y, font, color, colorForce, progress, maxWidth, align, dontDraw, lineHeight, outline, blend, samplerState, effect, dontSubstituteLocFont, letterSpacing, extraScale);
	}

	private static List<Deck> NewRunOptions_CharSelect_Transpiler_ModifyAllChars(List<Deck> allChars)
		=> allChars.Skip(ScrollPosition).Take(MaxCharactersOnScreen).ToList();
}

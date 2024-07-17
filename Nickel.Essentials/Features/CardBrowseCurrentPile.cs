using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Nickel.Essentials;

internal sealed partial class ProfileSettings
{
	[JsonProperty]
	public CardBrowseCurrentPileSetting CardBrowseCurrentPile = CardBrowseCurrentPileSetting.Both;
}

public enum CardBrowseCurrentPileSetting
{
	Off, Tooltip, Icon, Both
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class CardBrowseCurrentPile
{
	private static ISpriteEntry InDrawPileIcon = null!;
	private static ISpriteEntry InDiscardPileIcon = null!;
	private static ISpriteEntry InExhaustPileIcon = null!;

	private static bool IsRenderingCardBrowse;

	public static void ApplyPatches(IHarmony harmony)
	{
		InDrawPileIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/InDrawPile.png"));
		InDiscardPileIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/InDiscardPile.png"));
		InExhaustPileIcon = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/InExhaustPile.png"));

		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.Render)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_Render_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_Render_Finalizer))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_Render_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetAllTooltips)),
			postfix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetAllTooltips_Postfix)), priority: Priority.Last)
		);
	}

	public static IModSettingsApi.IModSetting MakeSettings(IModSettingsApi api)
		=> api.MakeEnumStepper(
			title: () => ModEntry.Instance.Localizations.Localize(["cardBrowseCurrentPile", "setting", "name"]),
			getter: () => ModEntry.Instance.Settings.ProfileBased.Current.CardBrowseCurrentPile,
			setter: value => ModEntry.Instance.Settings.ProfileBased.Current.CardBrowseCurrentPile = value
		).SetValueFormatter(
			value => ModEntry.Instance.Localizations.Localize(["cardBrowseCurrentPile", "setting", "value", value.ToString()])
		).SetValueWidth(
			_ => 60
		).SetTooltips(() => [
			new GlossaryTooltip($"settings.{ModEntry.Instance.Package.Manifest.UniqueName}::{MethodBase.GetCurrentMethod()!.DeclaringType!.Name}::Setting")
			{
				TitleColor = Colors.textBold,
				Title = ModEntry.Instance.Localizations.Localize(["cardBrowseCurrentPile", "setting", "name"]),
				Description = ModEntry.Instance.Localizations.Localize(["cardBrowseCurrentPile", "setting", "description"])
			}
		]);

	private static CardDestination? GetCardCurrentPile(State state, Combat? combat, Card card)
	{
		if (state.deck.Contains(card))
			return CardDestination.Deck;
		if (combat?.hand.Contains(card) ?? false)
			return CardDestination.Hand;
		if (combat?.discard.Contains(card) ?? false)
			return CardDestination.Discard;
		if (combat?.exhausted.Contains(card) ?? false)
			return CardDestination.Exhaust;
		return null;
	}

	private static Spr? GetCardDestinationIcon(CardDestination? destination)
		=> destination switch
		{
			CardDestination.Deck => InDrawPileIcon.Sprite,
			CardDestination.Hand => StableSpr.icons_dest_hand,
			CardDestination.Discard => InDiscardPileIcon.Sprite,
			CardDestination.Exhaust => InExhaustPileIcon.Sprite,
			_ => null
		};

	private static Tooltip? GetCardDestinationTooltip(CardDestination? destination)
	{
		var suffix = destination switch
		{
			CardDestination.Deck => "InDrawPile",
			CardDestination.Hand => "InHand",
			CardDestination.Discard => "InDiscardPile",
			CardDestination.Exhaust => "InExhaustPile",
			_ => null
		};
		if (suffix is null)
			return null;

		return new GlossaryTooltip($"{ModEntry.Instance.Package.Manifest.UniqueName}::CurrentPile::{suffix}")
		{
			Icon = GetCardDestinationIcon(destination),
			TitleColor = Colors.keyword,
			Title = ModEntry.Instance.Localizations.Localize(["cardBrowseCurrentPile", suffix, "name"]),
			Description = ModEntry.Instance.Localizations.Localize(["cardBrowseCurrentPile", suffix, "description"]),
		};
	}

	private static void CardBrowse_Render_Prefix(CardBrowse __instance)
		=> IsRenderingCardBrowse = __instance.subRoute is null;

	private static void CardBrowse_Render_Finalizer()
		=> IsRenderingCardBrowse = false;

	private static void Card_Render_Postfix(Card __instance, G g, Vec? posOverride)
	{
		if (!IsRenderingCardBrowse)
			return;
		if (ModEntry.Instance.Settings.ProfileBased.Current.CardBrowseCurrentPile is CardBrowseCurrentPileSetting.Off or CardBrowseCurrentPileSetting.Tooltip)
			return;
		if (g.state.route is not Combat combat)
			return;
		if (GetCardDestinationIcon(GetCardCurrentPile(g.state, combat, __instance)) is not { } icon)
			return;
		if (SpriteLoader.Get(icon) is not { } texture)
			return;

		var position = posOverride ?? __instance.pos;
		position += new Vec(0.0, __instance.hoverAnim * -2.0 + Mutil.Parabola(__instance.flipAnim) * -10.0 + Mutil.Parabola(Math.Abs(__instance.flopAnim)) * -10.0 * Math.Sign(__instance.flopAnim));
		position = position.round();

		Draw.Sprite(icon, position.x + 28 - texture.Width / 2, position.y + 75);
	}

	private static void Card_GetAllTooltips_Postfix(Card __instance, G g, ref IEnumerable<Tooltip> __result)
	{
		if (!IsRenderingCardBrowse)
			return;
		if (ModEntry.Instance.Settings.ProfileBased.Current.CardBrowseCurrentPile is CardBrowseCurrentPileSetting.Off or CardBrowseCurrentPileSetting.Icon)
			return;
		if (g.state.route is not Combat combat)
			return;
		if (GetCardDestinationTooltip(GetCardCurrentPile(g.state, combat, __instance)) is not { } tooltip)
			return;

		__result = [tooltip, .. __result];
	}
}

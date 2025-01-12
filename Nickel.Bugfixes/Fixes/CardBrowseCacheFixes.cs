using HarmonyLib;
using Nickel.Models.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nickel.Bugfixes;

internal static class CardBrowseCacheFixes
{
	private static CardBrowse? LastCardBrowse;
	private static CardBrowse? CardBrowseGettingCardList;
	private static readonly Dictionary<Card, CardData> CardDataCache = new();
	private static readonly Dictionary<Card, IReadOnlyDictionary<ICardTraitEntry, CardTraitState>> CardTraitStateCache = new();

	public static void ApplyPatches(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.GetCardList))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(CardBrowse)}.{nameof(CardBrowse.GetCardList)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Finalizer))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetDataWithOverrides))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Card)}.{nameof(Card.GetDataWithOverrides)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Card_GetDataWithOverrides_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Mod).Assembly.GetType("Nickel.CardTraitManager"), "ObtainCardTraitStates")
			          ?? throw new InvalidOperationException("Could not patch game methods: missing method `Nickel.CardTraitManager.ObtainCardTraitStates`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Nickel_CardTraitManager_ObtainCardTraitStates_Prefix))
		);
	}

	private static void CardBrowse_GetCardList_Prefix(CardBrowse __instance, G g)
	{
		if (__instance.browseAction is not null || __instance.GetType() != typeof(CardBrowse))
			return;

		if (LastCardBrowse == __instance)
		{
			CardBrowseGettingCardList = __instance;
			return;
		}

		LastCardBrowse = __instance;
		CardDataCache.Clear();
		CardTraitStateCache.Clear();
		
		foreach (var card in DB.releasedCards)
		{
			CardDataCache[card] = card.GetDataWithOverrides(g.state);
			CardTraitStateCache[card] = ModEntry.Instance.Helper.Content.Cards.GetAllCardTraits(g.state, card).ToDictionary();
		}
		
		CardBrowseGettingCardList = __instance;
	}

	private static void CardBrowse_GetCardList_Finalizer()
		=> CardBrowseGettingCardList = null;

	private static bool Card_GetDataWithOverrides_Prefix(Card __instance, ref CardData __result)
	{
		if (CardBrowseGettingCardList is not { browseAction: null } route || route.GetType() != typeof(CardBrowse))
			return true;
		
		__result = CardDataCache.GetValueOrDefault(__instance);
		return false;
	}

	private static bool Nickel_CardTraitManager_ObtainCardTraitStates_Prefix(Card card, ref IReadOnlyDictionary<ICardTraitEntry, CardTraitState> __result)
	{
		if (CardBrowseGettingCardList is not { browseAction: null } route || route.GetType() != typeof(CardBrowse))
			return true;
		
		__result = CardTraitStateCache.GetValueOrDefault(card) ?? new Dictionary<ICardTraitEntry, CardTraitState>();
		return false;
	}
}

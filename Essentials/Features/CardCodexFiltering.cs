using daisyowl.text;
using FSPRO;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nickel.Essentials;

internal static class CardCodexFiltering
{
	private const UK DeckFilterKey = (UK)2137301;

	private static List<Deck> DeckTypes = [];
	private static readonly HashSet<Deck> FilteredOutDecks = [];

	public static void ApplyPatches(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.GetCardList)),
			postfix: new HarmonyMethod(typeof(CardCodexFiltering), nameof(CardBrowse_GetCardList_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.Render)),
			prefix: new HarmonyMethod(typeof(CardCodexFiltering), nameof(CardBrowse_Render_Prefix)),
			postfix: new HarmonyMethod(typeof(CardCodexFiltering), nameof(CardBrowse_Render_Postfix))
		);
	}

	private static void CardBrowse_GetCardList_Postfix(CardBrowse __instance, G g, ref List<Card> __result)
	{
		if (__instance.browseSource != CardBrowse.Source.Codex)
			return;
		__result = __result
			.Where(c => !FilteredOutDecks.Contains(c.GetMeta().deck))
			.ToList();
	}

	private static void CardBrowse_Render_Prefix(CardBrowse __instance, G g)
	{
		if (__instance.browseSource != CardBrowse.Source.Codex)
			return;
		if (!__instance._needsCardInit)
			return;

		FilteredOutDecks.Clear();
		DeckTypes = __instance.GetCardList(g)
			.Select(c => c.GetMeta().deck)
			.Distinct()
			.OrderBy(d => (int)d)
			.ToList();
	}

	private static void CardBrowse_Render_Postfix(CardBrowse __instance, G g)
	{
		if (__instance.browseSource != CardBrowse.Source.Codex)
			return;

		var fixedScroll = Math.Min(-Math.Round(__instance.scroll), (DeckTypes.Count + 1) / 2 * 16 - 208);
		if (fixedScroll < 0)
			fixedScroll = 0;

		for (var i = 0; i < DeckTypes.Count; i++)
		{
			var deck = DeckTypes[i];

			string GetNiceName()
			{
				var locKey = $"char.{deck.Key()}";
				if (DB.currentLocale.strings.TryGetValue(locKey, out var localized))
					return localized;

				var match = new Regex("\\w+$").Match(deck.Key());
				if (match.Success)
					return match.Value;

				return ((int)deck).ToString();
			}

			var niceName = GetNiceName();
			var colorMatch = new Regex("<c=.*?>(.*?)</c>").Match(niceName);
			if (colorMatch.Success)
				niceName = colorMatch.Groups[1].Value;

			var box = g.Push(
				new UIKey(DeckFilterKey, i),
				new Rect(i % 2 * 56 + 4, 54 + i / 2 * 16 - fixedScroll, 48, 16),
				onMouseDown: new MouseDownHandler(() =>
				{
					Audio.Play(Event.Click);
					if (Input.shift)
					{
						if (DeckTypes.Except(FilteredOutDecks).SequenceEqual([deck]))
						{
							FilteredOutDecks.Clear();
						}
						else
						{
							FilteredOutDecks.Clear();
							foreach (var anyDeck in DeckTypes)
								if (anyDeck != deck)
									FilteredOutDecks.Add(anyDeck);
						}
					}
					else if (FilteredOutDecks.Count == 0)
					{
						FilteredOutDecks.Clear();
						foreach (var anyDeck in DeckTypes)
							if (anyDeck != deck)
								FilteredOutDecks.Add(anyDeck);
					}
					else
					{
						if (!FilteredOutDecks.Remove(deck))
							FilteredOutDecks.Add(deck);
						if (DeckTypes.Except(FilteredOutDecks).SequenceEqual([]))
							FilteredOutDecks.Clear();
					}
				})
			);
			var sprite = FilteredOutDecks.Contains(deck) ? Spr.buttons_select_gray : Spr.buttons_select_gray_on;
			Draw.Sprite(sprite, box.rect.x, box.rect.y, scale: new Vec(0.875, 0.875), color: Colors.buttonBoxNormal);
			Draw.Text(
				$"<c={DB.decks[deck].color}>{niceName}</c>",
				box.rect.x + box.rect.w / 0.875 / 2,
				box.rect.y + 4.5 + (niceName.Length >= 12 ? 1 : 0),
				align: TAlign.Center,
				extraScale: niceName.Length >= 12 ? 0.75 : 1
			);
			g.Pop();
		}
	}

	private sealed record MouseDownHandler(Action Delegate) : OnMouseDown
	{
		public void OnMouseDown(G _1, Box _2)
			=> this.Delegate();
	}
}

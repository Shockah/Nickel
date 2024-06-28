using daisyowl.text;
using FSPRO;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nickel.Essentials;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static partial class CardCodexFiltering
{
	private static readonly UK DeckFilterKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

	private static List<Deck> DeckTypes = [];
	private static readonly HashSet<Deck> FilteredOutDecks = [];
	private static readonly Dictionary<Deck, string> DeckNiceNames = [];
	private static double FilterScroll;
	private static double FilterScrollTarget;

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

	private static string ObtainDeckNiceName(Deck deck)
	{
		if (!DeckNiceNames.TryGetValue(deck, out var niceName))
		{
			static string GetNiceName(Deck deck)
			{
				var locKey = $"char.{deck.Key()}";
				if (DB.currentLocale.strings.TryGetValue(locKey, out var localized))
					return localized;

				var match = LastWordRegex().Match(deck.Key());
				if (match.Success)
				{
					var text = match.Value;
					if (text.Length >= 1)
						text = string.Concat(text[0].ToString().ToUpper(), text.AsSpan(1));
					return text;
				}

				return ((int)deck).ToString();
			}

			niceName = GetNiceName(deck);
			DeckNiceNames[deck] = niceName;
		}
		return niceName;
	}

	private static void CardBrowse_GetCardList_Postfix(CardBrowse __instance, ref List<Card> __result)
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

		FilterScroll = 0;
		FilterScrollTarget = 0;

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
		if (__instance.subRoute is not null)
			return;

		var topOffset = 54;
		var bottomOffset = 54;
		var scale = 0.875;
		var maxFilterScroll = Math.Max((DeckTypes.Count + 1) / 2 * 16 - 253, 0) * 2;

		for (var i = 0; i < DeckTypes.Count; i++)
		{
			var deck = DeckTypes[i];
			var niceName = ObtainDeckNiceName(deck);
			var colorMatch = UncoloredRegex().Match(niceName);
			if (colorMatch.Success)
				niceName = colorMatch.Groups[1].Value;

			var box = g.Push(
				new UIKey(DeckFilterKey, i),
				new Rect(i % 2 * 56 + 4, bottomOffset + i / 2 * 16 + FilterScroll, 48 / scale, 16),
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
			var sprite = FilteredOutDecks.Contains(deck) ? StableSpr.buttons_select_gray : StableSpr.buttons_select_gray_on;
			Draw.Sprite(sprite, box.rect.x, box.rect.y, scale: new Vec(scale, scale), color: Colors.buttonBoxNormal);
			Draw.Text(
				$"<c={DB.decks[deck].color}>{niceName}</c>",
				box.rect.x + box.rect.w / 2,
				box.rect.y + 4.5 + (niceName.Length >= 12 ? 1 : 0),
				align: TAlign.Center,
				extraScale: niceName.Length >= 12 ? 0.75 : 1
			);
			if (Input.gamepadIsActiveInput && Equals(box.key, Input.currentGpKey))
			{
				if (box.rect.y < topOffset)
					FilterScrollTarget = Math.Round(-(box.rect.y - FilterScroll - topOffset));
				if (box.rect.y2 > g.mg.PIX_H - bottomOffset)
					FilterScrollTarget = Math.Round(-(box.rect.y2 - FilterScroll - g.mg.PIX_H + bottomOffset));
			}
			g.Pop();
		}

		ScrollUtils.ReadScrollInputAndUpdate(g.dt, maxFilterScroll, ref FilterScroll, ref FilterScrollTarget);
	}

	[GeneratedRegex("\\w+$")]
	private static partial Regex LastWordRegex();

	[GeneratedRegex("<c=.*?>(.*?)</c>")]
	private static partial Regex UncoloredRegex();
}

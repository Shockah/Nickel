using FSPRO;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Nickel.Essentials;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class LogbookReplacement
{
	private static readonly UK SelectCharacterKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();
	private static readonly Dictionary<string, Card?> CardKeyToInstance = [];

	public static void ApplyPatches(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(LogBook), nameof(LogBook.Render))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(LogBook)}.{nameof(LogBook.Render)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(LogBook_Render_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Route), nameof(Route.TryCloseSubRoute))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Route)}.{nameof(Route.TryCloseSubRoute)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Route_TryCloseSubRoute_Postfix))
		);
	}

	private static bool LogBook_Render_Prefix(LogBook __instance, G g)
	{
		if (NewRunOptions.allChars.All(deck => ModEntry.Instance.Helper.Content.Decks.LookupByDeck(deck)?.ModOwner == ModEntry.Instance.Helper.ModRegistry.VanillaModManifest))
			return true;
		
		var selectedCharacters = ModEntry.Instance.Helper.ModData.ObtainModData<List<Deck>>(__instance, "SelectedCharacters");
		var runCounts = ModEntry.Instance.Helper.ModData.ObtainModData<Dictionary<Deck, int>>(__instance, "RunCounts");
		var winCounts = ModEntry.Instance.Helper.ModData.ObtainModData<Dictionary<Deck, int>>(__instance, "WinCounts");
		var highestWins = ModEntry.Instance.Helper.ModData.ObtainModData<Dictionary<(Deck, Deck, Deck), int?>>(__instance, "HighestWins");
		var lastGpKey = ModEntry.Instance.Helper.ModData.GetModDataOrDefault<UIKey?>(__instance, "LastGpKey");
		var subroute = ModEntry.Instance.Helper.ModData.GetOptionalModData<Route>(__instance, "Subroute");
		var unlockedChars = g.state.storyVars.GetUnlockedChars();
		
		if (subroute is not null)
		{
			subroute.Render(g);
			return false;
		}
		
		ScrollUtils.ReadScrollInputAndUpdate(g.dt, (int)__instance.maxY - MG.inst.PIX_H - 30, ref __instance.scroll, ref __instance.scrollTarget);
		SharedArt.DrawEngineering(g);

		var margin = 82;
		var characterWidth = 35;
		var characterHeight = 33;
		var cardWidth = 6;
		var cardHeight = 8;
		var comboWidth = 8;
		var comboHeight = 17;
		var sectionSpacing = 8;
		
		var width = MG.inst.PIX_W - margin * 2;
		var box = g.Push(rect: new(margin, 12 + (int)__instance.scroll, width), onInputPhase: __instance);
		var totalHeight = 0;

		totalHeight += RenderCharacterSelector() + sectionSpacing;
		totalHeight += RenderSelectedCharacterRunDetails() is { } runDetailsHeight ? runDetailsHeight + sectionSpacing : 0;
		totalHeight += RenderSeenCards() + sectionSpacing;
		totalHeight += RenderCombos();
		
		g.Pop();
		SharedArt.ButtonText(g, new(413, 228), StableUK.logbook_back, Loc.T("uiShared.btnBack"), onMouseDown: __instance, platformButtonHint: Btn.B);
		
		__instance.maxY = totalHeight + 80;
		
		if (Input.gamepadIsActiveInput)
		{
			if (Input.currentGpKey != lastGpKey && Input.currentGpKey is { } currentGpKey && g.boxes.FirstOrDefault(b => b.key == currentGpKey) is { } currentGpBox)
			{
				var scrolled = currentGpBox.rect;
				var target = scrolled;

				if (target.y2 > MG.inst.PIX_H - 60)
					target.y = MG.inst.PIX_H - 60 - target.h;
				if (target.y < 60)
					target.y = 60;

				__instance.scrollTarget = target.y + (int)__instance.scroll - scrolled.y;
				__instance.scrollTarget = Math.Clamp(__instance.scrollTarget, -__instance.maxY, 0);
			}
			ModEntry.Instance.Helper.ModData.SetModData(__instance, "LastGpKey", g.hoverKey);
		}
		
		return false;

		int ObtainRunCount(Deck deck)
		{
			if (!runCounts.TryGetValue(deck, out var value))
			{
				value = g.state.bigStats.combos
					.Where(kvp => BigStats.ParseComboKey(kvp.Key)?.decks.Contains(deck) == true)
					.Sum(kvp => kvp.Value.runs);
				runCounts[deck] = value;
			}
			return value;
		}

		int ObtainWinCount(Deck deck)
		{
			if (!winCounts.TryGetValue(deck, out var value))
			{
				value = g.state.bigStats.combos
					.Where(kvp => BigStats.ParseComboKey(kvp.Key)?.decks.Contains(deck) == true)
					.Sum(kvp => kvp.Value.wins);
				winCounts[deck] = value;
			}
			return value;
		}

		int? ObtainHighestWin(Deck first, Deck second, Deck third)
		{
			if (!highestWins.TryGetValue((first, second, third), out var value))
			{
				value = g.state.bigStats.combos
					.Where(kvp => kvp.Value.maxDifficultyWin is not null)
					.FirstOrNull(kvp =>
					{
						if (BigStats.ParseComboKey(kvp.Key) is not { } parsedKey)
							return false;
						return parsedKey.decks.Contains(first) && parsedKey.decks.Contains(second) && parsedKey.decks.Contains(third);
					})?.Value.maxDifficultyWin;
				highestWins[(first, second, third)] = value;
			}
			return value;
		}

		Card? ObtainCard(string key)
		{
			if (!CardKeyToInstance.TryGetValue(key, out var card))
			{
				card = DB.cards.TryGetValue(key, out var cardType) ? (Card?)Activator.CreateInstance(cardType) : null;
				CardKeyToInstance[key] = card;
			}
			return card;
		}

		int RenderCharacterSelector()
		{
			var perRow = (int)(box.rect.w / characterWidth);
			var rows = NewRunOptions.allChars.Where(deck => unlockedChars.Contains(deck)).Chunk(perRow).ToList();
			
			for (var y = 0; y < rows.Count; y++)
			{
				var row = rows[y];
				for (var x = 0; x < row.Length; x++)
				{
					var deck = row[x];
					var character = new Character { type = deck.Key(), deckType = deck };
					var characterUiKey = new UIKey(SelectCharacterKey, (int)deck, character.type);
					character.Render(g, x * characterWidth, y * characterHeight, mini: true, isSelected: selectedCharacters.Contains(deck), autoFocus: true, onMouseDown: new MouseDownHandler(() =>
					{
						Audio.Play(Event.Click);

						if (Input.shift)
						{
							selectedCharacters.Clear();
							selectedCharacters.Add(deck);
						}
						else if (!selectedCharacters.Remove(deck))
						{
							if (selectedCharacters.Count >= 3)
								selectedCharacters.RemoveAt(0);
							selectedCharacters.Add(deck);
						}
					}), overrideKey: characterUiKey);
				}
			}

			return rows.Count * characterHeight;
		}

		int? RenderSelectedCharacterRunDetails()
		{
			if (selectedCharacters.Count == 0)
				return null;
			
			for (var i = 0; i < selectedCharacters.Count; i++)
			{
				var deck = selectedCharacters[i];
				var character = new Character { deckType = deck, type = deck.Key() };
				character.Render(g, i * 88, totalHeight, mini: true, autoFocus: true, showTooltips: false);
				Draw.Text(Character.GetDisplayName(deck.Key(), g.state), box.rect.x + 38.0 + i * 88, box.rect.y + totalHeight + 5.0, color: Colors.textBold, font: DB.pinch);
				Draw.Text($"{ObtainWinCount(deck)} {Loc.T("logBook.charWins", "Wins")}", box.rect.x + 38.0 + i * 88, box.rect.y + totalHeight + 14.0, null, Colors.textMain);
				Draw.Text($"{ObtainRunCount(deck)} {Loc.T("logBook.charRuns", "Runs")}", box.rect.x + 38.0 + i * 88, box.rect.y + totalHeight + 23.0, null, Colors.textFaint);
			}
			return characterHeight;
		}

		int RenderSeenCards()
		{
			var perRow = (width + 1) / (cardWidth + 1);
			var rows = DB.cardMetas
				.Where(kvp => kvp.Value is { dontOffer: false, unreleased: false })
				.Where(kvp => (selectedCharacters.Count == 0 && NewRunOptions.allChars.Contains(kvp.Value.deck)) || selectedCharacters.Contains(kvp.Value.deck))
				.Select(kvp => kvp.Key)
				.Select(ObtainCard)
				.Where(card => card is not null)
				.Select(card => card!)
				.OrderBy(card => NewRunOptions.allChars.IndexOf(card.GetMeta().deck))
				.ThenBy(card => card.GetFullDisplayName())
				.Chunk(perRow)
				.ToList();
			
			for (var y = 0; y < rows.Count; y++)
			{
				var row = rows[y];
				for (var x = 0; x < row.Length; x++)
				{
					var card = row[x];
					var hasIt = g.state.storyVars.cardsOwned.Contains(card.Key());
					var uiKey = new UIKey(StableUK.logbook_card, 0, DB.Join(card.GetMeta().deck.Key(), card.Key()));
					var box = g.Push(uiKey, new(x * (cardWidth + 1), totalHeight + y * (cardHeight + 1), cardWidth, cardHeight), onMouseDownRight: new MouseDownHandler(() =>
					{
						if (!hasIt)
							return;
						
						ModEntry.Instance.Helper.ModData.SetOptionalModData<Route>(__instance, "Subroute", new CardUpgrade
						{
							cardCopy = Mutil.DeepCopy(card),
							isPreview = true
						});
					}));
					
					Draw.Sprite(StableSpr.miscUI_log_card, box.rect.x, box.rect.y, color: DB.decks[card.GetMeta().deck].color.gain(hasIt ? 1.0 : 0.35));
					if (box.IsHover())
						g.tooltips.Add(box.rect.xy + new Vec(10, 10), hasIt ? new TTCard { card = card } : new TTText { text = "???" });
					
					g.Pop();
				}
			}

			return rows.Count * (cardHeight + 1) - 1;
		}

		int RenderCombos()
		{
			var perRow = (width + 1) / (comboWidth + 1);
			var rows = GetCombos().Chunk(perRow).ToList();
			
			for (var y = 0; y < rows.Count; y++)
			{
				var row = rows[y];
				for (var x = 0; x < row.Length; x++)
				{
					var combo = row[x];
					var highestWin = ObtainHighestWin(combo.Item1, combo.Item2, combo.Item3);
					
					var uiKey = new UIKey(StableUK.logbook_combo, 0, $"{combo.Item1.Key()},{combo.Item2.Key()},{combo.Item3.Key()}");
					var box = g.Push(uiKey, new(x * (comboWidth + 1), totalHeight + y * (comboHeight + 3), comboWidth, comboHeight));

					if (highestWin is not null)
						Draw.Sprite(StableSpr.miscUI_combo_difficulty, box.rect.x, box.rect.y, color: NewRunOptions.GetDifficultyColorLogbook(highestWin.Value));
					Draw.Sprite(StableSpr.miscUI_combo_bar, box.rect.x, box.rect.y + 3, color: DB.decks[combo.Item1].color.gain(highestWin is null ? 0.35 : 1.0));
					Draw.Sprite(StableSpr.miscUI_combo_bar, box.rect.x, box.rect.y + 8, color: DB.decks[combo.Item2].color.gain(highestWin is null ? 0.35 : 1.0));
					Draw.Sprite(StableSpr.miscUI_combo_bar, box.rect.x, box.rect.y + 13, color: DB.decks[combo.Item3].color.gain(highestWin is null ? 0.35 : 1.0));

					if (box.IsHover())
					{
						var text = $"{Character.GetDisplayName(combo.Item1, g.state)}, {Character.GetDisplayName(combo.Item2, g.state)}, {Character.GetDisplayName(combo.Item3, g.state)}";
						if (highestWin is not null && NewRunOptions.difficulties.FirstOrDefault(difficulty => difficulty.level == highestWin.Value) is { } difficulty)
							text = $"<c={NewRunOptions.GetDifficultyColorLogbook(highestWin.Value).normalize().gain(1.5)}>{Loc.T(difficulty.locKey)}</c>\n{text}";
						g.tooltips.Add(box.rect.xy + new Vec(10.0, 10.0), new TTText { text = text });
					}
					
					g.Pop();
				}
			}
			
			return rows.Count * (comboHeight + 3) - 3;
		}

		IEnumerable<(Deck, Deck, Deck)> GetCombos()
		{
			var maybeFirstSelection = selectedCharacters.Count > 0 ? selectedCharacters[0] : (Deck?)null;
			var maybeSecondSelection = selectedCharacters.Count > 1 ? selectedCharacters[1] : (Deck?)null;
			var maybeThirdSelection = selectedCharacters.Count > 2 ? selectedCharacters[2] : (Deck?)null;
			var unlockedCharacters = NewRunOptions.allChars.Where(unlockedChars.Contains).ToList();

			for (var i = 0; i < unlockedCharacters.Count; i++)
			{
				var deck1 = unlockedCharacters[i];
				
				for (var j = i + 1; j < unlockedCharacters.Count; j++)
				{
					var deck2 = unlockedCharacters[j];
				
					for (var k = j + 1; k < unlockedCharacters.Count; k++)
					{
						var deck3 = unlockedCharacters[k];
				
						if (maybeFirstSelection is { } firstSelection && deck1 != firstSelection && deck2 != firstSelection && deck3 != firstSelection)
							continue;
						if (maybeSecondSelection is { } secondSelection && deck1 != secondSelection && deck2 != secondSelection && deck3 != secondSelection)
							continue;
						if (maybeThirdSelection is { } thirdSelection && deck1 != thirdSelection && deck2 != thirdSelection && deck3 != thirdSelection)
							continue;
						if (maybeFirstSelection is null && maybeSecondSelection is null && maybeThirdSelection is null && ObtainHighestWin(deck1, deck2, deck3) is null)
							continue;

						yield return (deck1, deck2, deck3);
					}
				}
			}
		}
	}

	private static void Route_TryCloseSubRoute_Postfix(Route __instance, Route r, ref bool __result)
	{
		if (__result)
			return;
		if (__instance is not LogBook)
			return;
		if (NewRunOptions.allChars.All(deck => ModEntry.Instance.Helper.Content.Decks.LookupByDeck(deck)?.ModOwner == ModEntry.Instance.Helper.ModRegistry.VanillaModManifest))
			return;
		
		var subroute = ModEntry.Instance.Helper.ModData.GetOptionalModData<Route>(__instance, "Subroute");
		if (r == subroute)
		{
			ModEntry.Instance.Helper.ModData.SetOptionalModData<Route>(__instance, "Subroute", null);
			__result = true;
			
			var lastGpKey = ModEntry.Instance.Helper.ModData.GetModDataOrDefault<UIKey?>(__instance, "LastGpKey");
			Input.currentGpKey = lastGpKey;
		}
	}
}

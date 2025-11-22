using FSPRO;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Nickel.Essentials;

internal static class LogbookReplacement
{
	internal readonly struct DeckCombo(HashSet<Deck> values) : IEquatable<DeckCombo>
	{
		public readonly HashSet<Deck> Values = values;

		public override bool Equals(object? obj)
			=> obj is DeckCombo other && this.Equals(other);

		public bool Equals(DeckCombo other)
			=> this.Values.SetEquals(other.Values);

		public override int GetHashCode()
			=> this.Values.Sum(deck => (int)deck);

		public string GetDisplayName(State state)
			=> this.Values.Count == 0
				? ModEntry.Instance.Localizations.Localize(["logbookReplacement", "unmanned"])
				: string.Join(", ", this.Values.OrderBy(deck => NewRunOptions.allChars.IndexOf(deck)).Select(deck => Character.GetDisplayName(deck, state))); // TODO: localize
	}
	
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
		var runCounts = ModEntry.Instance.Helper.ModData.ObtainModData<Dictionary<DeckCombo, int>>(__instance, "RunCounts");
		var winCounts = ModEntry.Instance.Helper.ModData.ObtainModData<Dictionary<DeckCombo, int>>(__instance, "WinCounts");
		var highestWins = ModEntry.Instance.Helper.ModData.ObtainModData<Dictionary<DeckCombo, int?>>(__instance, "HighestWins");
		var allCombos = ModEntry.Instance.Helper.ModData.ObtainModData<List<DeckCombo>>(__instance, "AllCombos");
		var currentCombos = ModEntry.Instance.Helper.ModData.GetOptionalModData<List<DeckCombo>>(__instance, "CurrentCombos");
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

		const int margin = 82;
		const int characterWidth = 35;
		const int characterHeight = 33;
		const int cardWidth = 6;
		const int cardHeight = 8;
		const int comboWidth = 8;
		const int comboHeight = 17;
		const int sectionSpacing = 8;
		
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

		int ObtainRunCount(DeckCombo combo)
		{
			ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(runCounts, combo, out var valueExists);
			if (!valueExists)
				value = g.state.bigStats.combos
					.Where(kvp => BigStats.ParseComboKey(kvp.Key) is { } parsedStats && combo.Values.All(deck => parsedStats.decks.Contains(deck)))
					.Sum(kvp => kvp.Value.runs);
			return value;
		}

		int ObtainWinCount(DeckCombo combo)
		{
			ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(winCounts, combo, out var valueExists);
			if (!valueExists)
				value = g.state.bigStats.combos
					.Where(kvp => BigStats.ParseComboKey(kvp.Key) is { } parsedStats && combo.Values.All(deck => parsedStats.decks.Contains(deck)))
					.Sum(kvp => kvp.Value.wins);
			return value;
		}

		int? ObtainHighestWinForExactCombo(DeckCombo combo)
		{
			ref var value = ref CollectionsMarshal.GetValueRefOrAddDefault(highestWins, combo, out var valueExists);
			if (!valueExists)
				value = g.state.bigStats.combos
					.Where(kvp =>
					{
						if (kvp.Value.maxDifficultyWin is null)
							return false;
						if (BigStats.ParseComboKey(kvp.Key) is not { } parsedKey)
							return false;
						if (!combo.Values.SetEquals(parsedKey.decks))
							return false;
						return true;
					})
					.Select(kvp => kvp.Value.maxDifficultyWin!.Value)
					.OrderDescending()
					.FirstOrNull();
			return value;
		}

		Card? ObtainCard(string key)
		{
			ref var card = ref CollectionsMarshal.GetValueRefOrAddDefault(CardKeyToInstance, key, out var cardExists);
			if (!cardExists)
				card = DB.cards.TryGetValue(key, out var cardType) ? (Card?)Activator.CreateInstance(cardType) : null;
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

						currentCombos = null;
						ModEntry.Instance.Helper.ModData.RemoveModData(__instance, "CurrentCombos");
					}), overrideKey: characterUiKey);
				}
			}

			return rows.Count * characterHeight;
		}

		int? RenderSelectedCharacterRunDetails()
		{
			if (selectedCharacters.Count == 0)
				return null;

			var comboSet = new HashSet<Deck>();
			for (var i = 0; i < selectedCharacters.Count; i++)
			{
				var deck = selectedCharacters[i];
				comboSet.Add(deck);
				var character = new Character { deckType = deck, type = deck.Key() };
				character.Render(g, i * 37, totalHeight, mini: true, autoFocus: true, showTooltips: false);
			}
			var combo = new DeckCombo(comboSet);

			var textX = box.rect.x + selectedCharacters.Count * 37 + 2;
			Draw.Text(combo.GetDisplayName(g.state), textX, box.rect.y + totalHeight + 5.0, color: Colors.textBold, font: DB.pinch);
			Draw.Text($"{ObtainWinCount(combo)} {Loc.T("logBook.charWins", "Wins")}", textX, box.rect.y + totalHeight + 14.0, null, Colors.textMain);
			Draw.Text($"{ObtainRunCount(combo)} {Loc.T("logBook.charRuns", "Runs")}", textX, box.rect.y + totalHeight + 23.0, null, Colors.textFaint);
			
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
			var rows = GetCurrentCombos().Chunk(perRow).ToList();
			
			for (var y = 0; y < rows.Count; y++)
			{
				var row = rows[y];
				for (var x = 0; x < row.Length; x++)
				{
					var combo = row[x];
					var highestWin = ObtainHighestWinForExactCombo(combo);
					var sortedCombo = combo.Values.OrderBy(d => NewRunOptions.allChars.IndexOf(d)).ToList();
					
					var uiKey = new UIKey(StableUK.logbook_combo, 0, string.Join(",", sortedCombo.Select(d => d.Key())));
					var box = g.Push(uiKey, new(x * (comboWidth + 1), totalHeight + y * (comboHeight + 3), comboWidth, comboHeight));

					if (highestWin is not null)
						Draw.Sprite(StableSpr.miscUI_combo_difficulty, box.rect.x, box.rect.y, color: NewRunOptions.GetDifficultyColorLogbook(highestWin.Value));
					for (var i = 0; i < Math.Min(sortedCombo.Count, 3); i++)
						Draw.Sprite(StableSpr.miscUI_combo_bar, box.rect.x, box.rect.y + 3 + i * 5, color: DB.decks[sortedCombo[i]].color.gain(highestWin is null ? 0.35 : 1.0));

					if (box.IsHover())
					{
						var text = string.Join(", ",sortedCombo.Select(d => Character.GetDisplayName(d, g.state)));
						if (highestWin is not null && NewRunOptions.difficulties.FirstOrDefault(difficulty => difficulty.level == highestWin.Value) is { } difficulty)
							text = $"<c={NewRunOptions.GetDifficultyColorLogbook(highestWin.Value).normalize().gain(1.5)}>{Loc.T(difficulty.locKey)}</c>\n{text}";
						g.tooltips.Add(box.rect.xy + new Vec(10.0, 10.0), new TTText { text = text });
					}
					
					g.Pop();
				}
			}
			
			return rows.Count * (comboHeight + 3) - 3;
		}

		IEnumerable<DeckCombo> GetAllCombos()
		{
			if (allCombos.Count != 0)
				return allCombos;
			
			var unlockedCharacters = NewRunOptions.allChars.Where(unlockedChars.Contains).ToList();
			var combo0 = new DeckCombo([]);
			var combos = new List<DeckCombo> { combo0 };

			for (var i = 0; i < unlockedCharacters.Count; i++)
			{
				var combo1 = new DeckCombo([unlockedCharacters[i]]); 
				combos.Add(combo1);
				
				for (var j = i + 1; j < unlockedCharacters.Count; j++)
				{
					var combo2 = new DeckCombo([unlockedCharacters[i], unlockedCharacters[j]]);
					combos.Add(combo2);
					
					for (var k = j + 1; k < unlockedCharacters.Count; k++)
					{
						var combo3 = new DeckCombo([unlockedCharacters[i], unlockedCharacters[j], unlockedCharacters[k]]);
						combos.Add(combo3);
					}
				}
			}

			allCombos.Clear();
			allCombos = combos
				.OrderBy(combo => combo.Values.Count)
				.ToList();

			return allCombos;
		}

		IEnumerable<DeckCombo> GetCurrentCombos()
		{
			if (currentCombos is not null)
				return currentCombos;

			currentCombos = GetAllCombos()
				.Where(combo =>
				{
					if (selectedCharacters.Count == 0)
						return ObtainHighestWinForExactCombo(combo) is not null;
					return combo.Values.Count == 3 && selectedCharacters.All(combo.Values.Contains);
				})
				.ToList();
			ModEntry.Instance.Helper.ModData.SetOptionalModData(__instance, "CurrentCombos", currentCombos);

			return currentCombos;
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

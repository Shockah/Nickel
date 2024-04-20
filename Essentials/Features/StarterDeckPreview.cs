using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;

namespace Nickel.Essentials;

internal static class StarterDeckPreview
{
	public static void ApplyPatches(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.Render))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(NewRunOptions)}.{nameof(NewRunOptions.Render)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_Render_Postfix))
		);
	}

	private static void NewRunOptions_Render_Postfix(NewRunOptions __instance, G g)
	{
		if (__instance.subRoute is not null)
			return;
		if (!g.state.runConfig.IsValid(g))
			return;

		var fakeState = Mutil.DeepCopy(g.state);
		fakeState.slot = null;
		var shipTemplate = StarterShip.ships.TryGetValue(fakeState.runConfig.selectedShip, out var ship) ? ship : StarterShip.ships.Values.First();

		try
		{
			fakeState.PopulateRun(
				shipTemplate: shipTemplate,
				chars: fakeState.runConfig.selectedChars,
				difficulty: fakeState.runConfig.difficulty,
				seed: fakeState.seed,
				giveRunStartRewards: false
			);

			foreach (var card in fakeState.deck)
				card.GetActions(fakeState, DB.fakeCombat);
		}
		catch
		{
			return;
		}

		var cards = fakeState.deck
			.Select(card => (Card: card, Meta: card.GetMeta()))
			.OrderBy(e => shipTemplate.cards.Any(shipCard => shipCard.Key() == e.Card.Key()) || e.Card.GetMeta().dontOffer)
			.ThenBy(e => !NewRunOptions.allChars.Contains(e.Meta.deck))
			.ThenBy(e => NewRunOptions.allChars.IndexOf(e.Meta.deck))
			.ThenBy(e => ModEntry.Instance.Api.IsExeCardType(e.Card.GetType()))
			.ThenBy(e => e.Meta.rarity)
			.ThenBy(e => e.Card.GetFullDisplayName())
			.Select(e => e.Card)
			.ToList();

		for (var i = 0; i < cards.Count; i++)
		{
			var card = cards[i];
			var isNonCatExe = ModEntry.Instance.Api.GetDeckForExeCardType(card.GetType()) is { } exeDeck && exeDeck != Deck.colorless;

			if (isNonCatExe)
				card = new ColorlessDizzySummon();

			var rect = new Rect((int)(MG.inst.PIX_W / 2 - 1 + i * 7 - cards.Count * 3.5), 48, 6, 8);
			var box = g.Push(new UIKey(StableUK.logbook_card, i), rect, onMouseDownRight: new MouseDownHandler(() =>
			{
				__instance.subRoute = new CardUpgrade
				{
					cardCopy = Mutil.DeepCopy(card),
					isPreview = true
				};
			}));

			Draw.Sprite(StableSpr.miscUI_log_card, rect.x, rect.y, color: DB.decks[card.GetMeta().deck].color);
			if (box.IsHover())
			{
				var tooltipPosition = box.rect.xy + new Vec(10, 10);
				if (isNonCatExe)
					g.tooltips.Add(tooltipPosition, ModEntry.Instance.Localizations.Localize(["starterDeckPreview", "exeCard"]));
				g.tooltips.Add(tooltipPosition, new TTCard { card = card });
			}

			g.Pop();
		}
	}
}

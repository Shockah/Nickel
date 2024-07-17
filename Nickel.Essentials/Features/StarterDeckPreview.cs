using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Nickel.Essentials;

internal sealed partial class ProfileSettings
{
	[JsonProperty]
	public bool StarterDeckPreview = true;
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class StarterDeckPreview
{
	private static bool UpdateNextFrame = true;
	private static Character? LastRenderedCharacter;
	private static readonly List<Card> LastStarterCards = [];
	private static readonly List<Artifact> LastStarterArtifacts = [];

	public static void ApplyPatches(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(G), nameof(G.BubbleEvents))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(G)}.{nameof(G.BubbleEvents)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(G_BubbleEvents_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.OnEnter))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(NewRunOptions)}.{nameof(NewRunOptions.OnEnter)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_OnEnter_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.Render))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(NewRunOptions)}.{nameof(NewRunOptions.Render)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_Render_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Character), nameof(Character.Render))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Character)}.{nameof(Character.Render)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Character_Render_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Artifact), nameof(Artifact.RenderArtifactList))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Artifact)}.{nameof(Artifact.RenderArtifactList)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Artifact_RenderArtifactList_Prefix))
		);
	}

	public static IModSettingsApi.IModSetting MakeSettings(IModSettingsApi api)
		=> api.MakeCheckbox(
			title: () => ModEntry.Instance.Localizations.Localize(["starterDeckPreview", "setting", "name"]),
			getter: () => ModEntry.Instance.Settings.ProfileBased.Current.StarterDeckPreview,
			setter: (_, _, value) => ModEntry.Instance.Settings.ProfileBased.Current.StarterDeckPreview = value
		);

	private static void UpdateStateBasedData(G g)
	{
		var fakeState = Mutil.DeepCopy(g.state);
		fakeState.slot = null;
		var shipTemplate = StarterShip.ships.TryGetValue(fakeState.runConfig.selectedShip, out var ship) ? ship : StarterShip.ships.Values.First();

		var oldDemo = FeatureFlags.Demo;
		try
		{
			FeatureFlags.Demo = DemoMode.PAX;
			ModEntry.StopStateTransitions = true;

			try
			{
				fakeState.PopulateRun(
					shipTemplate: shipTemplate,
					newMap: new MapDemo(),
					chars: NewRunOptions.allChars.Where(d => d != Deck.colorless),
					difficulty: fakeState.runConfig.hardmode ? fakeState.runConfig.difficulty : 0,
					seed: fakeState.seed,
					giveRunStartRewards: true
				);

				LastStarterArtifacts.Clear();
				LastStarterArtifacts.AddRange(fakeState.characters.SelectMany(c => c.artifacts));
			}
			catch
			{
				// ignored
			}

			if (!g.state.runConfig.IsValid(g))
				return;

			try
			{
				fakeState.PopulateRun(
					shipTemplate: shipTemplate,
					newMap: new MapDemo(),
					chars: fakeState.runConfig.selectedChars,
					difficulty: fakeState.runConfig.hardmode ? fakeState.runConfig.difficulty : 0,
					seed: fakeState.seed,
					giveRunStartRewards: true
				);

				foreach (var card in fakeState.deck)
					card.GetActions(fakeState, DB.fakeCombat);
			}
			catch
			{
				return;
			}
		}
		finally
		{
			FeatureFlags.Demo = oldDemo;
			ModEntry.StopStateTransitions = false;
		}

		LastStarterCards.Clear();
		LastStarterCards.AddRange(
			fakeState.deck
				.Select(card => (Card: card, Meta: card.GetMeta()))
				.OrderBy(e => shipTemplate.cards.Any(shipCard => shipCard.Key() == e.Card.Key()) || e.Meta is { dontOffer: true, deck: Deck.colorless })
				.ThenBy(e => !NewRunOptions.allChars.Contains(e.Meta.deck))
				.ThenBy(e => NewRunOptions.allChars.IndexOf(e.Meta.deck))
				.ThenBy(e => ModEntry.Instance.Api.IsExeCardType(e.Card.GetType()))
				.ThenBy(e => e.Meta.rarity)
				.ThenBy(e => e.Card.GetFullDisplayName())
				.Select(e => e.Card)
		);
	}

	private static void G_BubbleEvents_Prefix(G __instance)
	{
		// ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
		if (__instance.state?.route is not NewRunOptions route)
			return;
		if (route.subRoute is not null)
			return;
		if (Input.mouseLeftDown || Input.mouseRightDown || Input.GetGpDown(Btn.A, consume: false) || Input.GetGpDown(Btn.X, consume: false))
			UpdateNextFrame = true;
	}

	private static void NewRunOptions_OnEnter_Postfix()
		=> UpdateNextFrame = true;

	private static void NewRunOptions_Render_Postfix(NewRunOptions __instance, G g)
	{
		if (!ModEntry.Instance.Settings.ProfileBased.Current.StarterDeckPreview)
			return;
		if (__instance.subRoute is not null)
			return;

		if (UpdateNextFrame)
		{
			UpdateNextFrame = false;
			UpdateStateBasedData(g);
		}

		if (!g.state.runConfig.IsValid(g))
			return;

		var textRect = Draw.Text(ModEntry.Instance.Localizations.Localize(["starterDeckPreview", "startingDeck"]), 96, 258, color: Colors.textBold);

		for (var i = 0; i < LastStarterCards.Count; i++)
		{
			var card = LastStarterCards[i];
			var isNonCatExe = ModEntry.Instance.Api.GetDeckForExeCardType(card.GetType()) is { } exeDeck && exeDeck != Deck.colorless;

			if (isNonCatExe)
				card = new ColorlessDizzySummon();

			var rect = new Rect(96 + textRect.w + 4 + i * 7, 256, 6, 8);
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

	private static void Character_Render_Prefix(Character __instance, G g, bool renderLocked)
	{
		if (__instance.deckType is not { } deck)
			return;
		if (g.metaRoute is not null)
			return;
		if (g.state.route is not NewRunOptions)
			return;
		if (renderLocked)
			return;
		
		LastRenderedCharacter = __instance;

		__instance.artifacts.AddRange(LastStarterArtifacts.Where(a => a.GetMeta().owner == deck));
	}

	private static void Artifact_RenderArtifactList_Prefix(G g, ref Vec offset)
	{
		if (LastRenderedCharacter?.deckType is not { } deck)
			return;
		if (g.metaRoute is not null)
			return;
		if (g.state.route is not NewRunOptions)
			return;

		if (NewRunOptions.allChars.IndexOf(deck) % 2 == 0)
			offset = new(-5, offset.y);
	}
}

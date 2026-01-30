using FSPRO;
using HarmonyLib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nickel.Essentials;

internal sealed partial class ProfileSettings
{
	[JsonProperty]
	public HashSet<Deck> BlacklistedExeStarters = [];
	
	[JsonProperty]
	public HashSet<Deck> BlacklistedExeOfferings = [];
}

internal static class ExeBlacklist
{
	private static readonly UK CannotBlacklistWarningKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

	private static double CannotBlacklistWarning;
	private static readonly Dictionary<string, Deck?> CardKeyToExeDeckCache = [];

	public static void ApplyPatches(IHarmony harmony)
	{
		harmony.Patch(
			original: typeof(DeckDef).GetNestedTypes(AccessTools.all).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<CreateDeckDefs>") && m.ReturnType == typeof(Card))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(DeckDef)}.{nameof(DeckDef.CreateDeckDefs)}.delegate`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(DeckDef_CreateDeckDefs_Delegate_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.Render))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(NewRunOptions)}.{nameof(NewRunOptions.Render)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_Render_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(NewRunOptions), nameof(NewRunOptions.OnMouseDown))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(NewRunOptions)}.{nameof(NewRunOptions.OnMouseDown)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(NewRunOptions_OnMouseDown_Prefix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(RunConfig), nameof(RunConfig.IsValid))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(RunConfig)}.{nameof(RunConfig.IsValid)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RunConfig_IsValid_Postfix))
		);
		harmony.Patch(
			original: typeof(CardReward).GetNestedTypes(AccessTools.all).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<GetOffering>") && m.ReturnType == typeof(bool))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(CardReward)}.{nameof(CardReward.GetOffering)}+WhereDelegate`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardReward_GetOffering_Delegate_Postfix))
		);
	}

	public static IModSettingsApi.IModSetting MakeSettings(IModSettingsApi api)
		=> api.MakeList([
			new CharactersModSetting
			{
				Title = () => ModEntry.Instance.Localizations.Localize(["exeBlacklist", "startingBlacklistSetting", "name"]),
				AllCharacters = () => GetAllExeCharacters().ToList(),
				IsSelected = deck => !ModEntry.Instance.Settings.ProfileBased.Current.BlacklistedExeStarters.Contains(deck),
				SetSelected = (route, deck, value) =>
				{
					var oldValue = !ModEntry.Instance.Settings.ProfileBased.Current.BlacklistedExeStarters.Contains(deck);
					if (value != oldValue && !value && GetNonBlacklistedExeCharacters().Count() <= 4)
					{
						route.ShowWarning(ModEntry.Instance.Localizations.Localize(["exeBlacklist", "cannotBlacklistWarning"]), 2);
						return;
					}

					if (value)
						ModEntry.Instance.Settings.ProfileBased.Current.BlacklistedExeStarters.Remove(deck);
					else
						ModEntry.Instance.Settings.ProfileBased.Current.BlacklistedExeStarters.Add(deck);
				},
				CharacterTooltips = GetExeCardTooltipsForCharacter,
			},
			new CharactersModSetting
			{
				Title = () => ModEntry.Instance.Localizations.Localize(["exeBlacklist", "offeringBlacklistSetting", "name"]),
				AllCharacters = () => GetAllExeCharacters().ToList(),
				IsSelected = deck => !ModEntry.Instance.Settings.ProfileBased.Current.BlacklistedExeOfferings.Contains(deck),
				SetSelected = (_, deck, value) =>
				{
					if (value)
						ModEntry.Instance.Settings.ProfileBased.Current.BlacklistedExeOfferings.Remove(deck);
					else
						ModEntry.Instance.Settings.ProfileBased.Current.BlacklistedExeOfferings.Add(deck);
				},
				CharacterTooltips = GetExeCardTooltipsForCharacter,
			},
		]);

	// private static Deck? ObtainExeDeck(Type cardType)
	// {
	// 	if (ModEntry.Instance.Helper.Content.Cards.LookupByCardType(cardType) is not { } entry)
	// 		return null;
	// 	return ObtainExeDeck(entry.UniqueName);
	// }

	private static Deck? ObtainExeDeck(Card card)
		=> ObtainExeDeck(card.Key());

	private static Deck? ObtainExeDeck(string cardKey)
	{
		if (CardKeyToExeDeckCache.TryGetValue(cardKey, out var exeDeck))
			return exeDeck;
		
		foreach (var def in DB.decks.Values)
		{
			if (def.exeCard?.Key() != cardKey)
				continue;
				
			exeDeck = def.deck;
			CardKeyToExeDeckCache[cardKey] = exeDeck;
			return exeDeck;
		}
		
		CardKeyToExeDeckCache[cardKey] = null;
		return null;
	}

	private static IEnumerable<Tooltip> GetExeCardTooltipsForCharacter(Deck deck)
	{
		if (!DB.decks.TryGetValue(deck, out var def) || def.exeCard is null)
			return [];
		
		return [
			new TTDivider(),
			new TTCard { card = def.exeCard },
		];
	}

	private static IEnumerable<Deck> GetAllExeCharacters()
		=> DB.decks.Values
			.Where(def => def.exeCard is not null)
			.Select(def => def.deck);

	private static IEnumerable<Deck> GetNonBlacklistedExeCharacters()
		=> GetAllExeCharacters().Where(d => !ModEntry.Instance.Settings.ProfileBased.Current.BlacklistedExeStarters.Contains(d));

	private static void DeckDef_CreateDeckDefs_Delegate_Postfix(ref Card? __result)
	{
		if (__result is null)
			return;
		// if (ModEntry.Instance.Helper.ModData.TryGetModData(MG.inst.g.state, "RunningDataCollectingPopulateRun", out bool isRunningDataCollectingPopulateRun) && isRunningDataCollectingPopulateRun)
		// 	return;
		if (ObtainExeDeck(__result) is not { } exeDeck)
			return;
		if (!ModEntry.Instance.Settings.ProfileBased.Current.BlacklistedExeStarters.Contains(exeDeck))
			return;

		__result = null;
	}

	private static void NewRunOptions_Render_Postfix(G g)
	{
		CannotBlacklistWarning = Math.Max(0, CannotBlacklistWarning - g.dt);
		if (CannotBlacklistWarning > 0)
			SharedArt.WarningPopup(g, CannotBlacklistWarningKey, ModEntry.Instance.Localizations.Localize(["exeBlacklist", "cannotBlacklistWarning"]), new Vec(240, 65));
	}

	private static bool NewRunOptions_OnMouseDown_Prefix(G g, Box b)
	{
		if (!g.state.runConfig.selectedChars.Contains(Deck.colorless))
			return true;
		// TODO: re-add alt starters support
		// if (ModEntry.Instance.MoreDifficultiesApi?.AreAltStartersEnabled(g.state, Deck.colorless) == true)
		// 	return true;
		if (b.key != StableUK.newRun_continue)
			return true;
		if (GetNonBlacklistedExeCharacters().Count(d => !g.state.runConfig.selectedChars.Contains(d)) >= 2)
			return true;

		CannotBlacklistWarning = 1.5;
		Audio.Play(Event.ZeroEnergy);
		return false;
	}

	private static void RunConfig_IsValid_Postfix(RunConfig __instance, G g, ref bool __result)
	{
		if (!__instance.selectedChars.Contains(Deck.colorless))
			return;
		// if (ModEntry.Instance.MoreDifficultiesApi?.AreAltStartersEnabled(g.state, Deck.colorless) == true)
		// 	return;
		if (!__result)
			return;
		if (GetNonBlacklistedExeCharacters().Count(d => !__instance.selectedChars.Contains(d)) < 2)
			__result = false;
	}

	private static void CardReward_GetOffering_Delegate_Postfix(Card c, ref bool __result)
	{
		if (!__result)
			return;
		if (ObtainExeDeck(c) is not { } exeDeck)
			return;
		if (!ModEntry.Instance.Settings.ProfileBased.Current.BlacklistedExeOfferings.Contains(exeDeck))
			return;

		__result = false;
	}
}

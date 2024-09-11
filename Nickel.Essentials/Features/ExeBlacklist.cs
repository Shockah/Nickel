using FSPRO;
using HarmonyLib;
using Microsoft.Extensions.Logging;
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
	public HashSet<Deck> BlacklistedExeStarters = [];
	
	[JsonProperty]
	public HashSet<Deck> BlacklistedExeOfferings = [];
}

internal static class ExeBlacklist
{
	private static readonly UK CannotBlacklistWarningKey = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<UK>();

	private static State? LastState;
	private static double CannotBlacklistWarning;

	public static void ApplyPatches(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(State), nameof(State.PopulateRun))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(State)}.{nameof(State.PopulateRun)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_PopulateRun_Prefix))
		);
		harmony.Patch(
			original: typeof(State).GetNestedTypes(AccessTools.all).SelectMany(t => t.GetMethods(AccessTools.all)).First(m => m.Name.StartsWith("<PopulateRun>") && m.ReturnType == typeof(Route))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(State)}.<compiler-generated-type>.<PopulateRun>`"),
			transpiler: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_PopulateRun_Delegate_Transpiler)), priority: Priority.First)
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
				}
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
				}
			},
		]);

	private static IEnumerable<Deck> GetAllExeCharacters()
		=> NewRunOptions.allChars.Where(d => d != Deck.colorless && ModEntry.Instance.Api.GetExeCardTypeForDeck(d) is not null);

	private static IEnumerable<Deck> GetNonBlacklistedExeCharacters()
		=> GetAllExeCharacters().Where(d => !ModEntry.Instance.Settings.ProfileBased.Current.BlacklistedExeStarters.Contains(d));

	private static void State_PopulateRun_Prefix(State __instance)
		=> LastState = __instance;

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> State_PopulateRun_Delegate_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("chars"),
					ILMatches.LdcI4((int)Deck.shard),
					ILMatches.Call("Contains"),
					ILMatches.Brtrue,
					ILMatches.Ldloc<List<Card>>(originalMethod).CreateLdlocInstruction(out var ldlocCards),
					ILMatches.Instruction(OpCodes.Newobj),
					ILMatches.Call("Add")
				)
				.PointerMatcher(SequenceMatcherRelativeElement.AfterLast)
				.ExtractLabels(out var labels)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					ldlocCards.Value.WithLabels(labels),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(State_PopulateRun_Delegate_Transpiler_ModifyPotentialExeCards)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.GetDisplayName(@long: false), ex);
			return instructions;
		}
	}

	private static void State_PopulateRun_Delegate_Transpiler_ModifyPotentialExeCards(List<Card> cards)
	{
		if (LastState is not { } state)
			return;
		if (ModEntry.Instance.Helper.ModData.TryGetModData(state, "RunningDataCollectingPopulateRun", out bool isRunningDataCollectingPopulateRun) && isRunningDataCollectingPopulateRun)
			return;

		for (var i = cards.Count - 1; i >= 0; i--)
			if (ModEntry.Instance.Api.GetDeckForExeCardType(cards[i].GetType()) is { } exeDeck)
				if (ModEntry.Instance.Settings.ProfileBased.Current.BlacklistedExeStarters.Contains(exeDeck))
					cards.RemoveAt(i);
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
		if (ModEntry.Instance.MoreDifficultiesApi?.AreAltStartersEnabled(g.state, Deck.colorless) == true)
			return true;
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
		if (ModEntry.Instance.MoreDifficultiesApi?.AreAltStartersEnabled(g.state, Deck.colorless) == true)
			return;
		if (!__result)
			return;
		if (GetNonBlacklistedExeCharacters().Count(d => !__instance.selectedChars.Contains(d)) < 2)
			__result = false;
	}

	private static void CardReward_GetOffering_Delegate_Postfix(Card c, ref bool __result)
	{
		if (!__result)
			return;
		if (ModEntry.Instance.Api.GetDeckForExeCardType(c.GetType()) is not { } exeDeck)
			return;
		if (!ModEntry.Instance.Settings.ProfileBased.Current.BlacklistedExeOfferings.Contains(exeDeck))
			return;

		__result = false;
	}
}

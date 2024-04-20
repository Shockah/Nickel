using FSPRO;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Essentials;

file static class RunConfigExt
{
	public static bool IsBlacklistedExe(this RunConfig self, Deck deck)
		=> ModEntry.Instance.Helper.ModData.TryGetModData<HashSet<string>>(self, "BlacklistedExes", out var blacklist) && blacklist.Contains(deck.Key());

	public static void SetBlacklistedExe(this RunConfig self, Deck deck, bool isBlacklisted)
	{
		var blacklist = ModEntry.Instance.Helper.ModData.ObtainModData<HashSet<string>>(self, "BlacklistedExes");
		if (isBlacklisted)
			blacklist.Add(deck.Key());
		else
			blacklist.Remove(deck.Key());
	}

	public static void ToggleBlacklistedExe(this RunConfig self, Deck deck)
		=> self.SetBlacklistedExe(deck, !self.IsBlacklistedExe(deck));
}

internal static class ExeBlacklist
{
	private const UK ExeBlacklistKey = (UK)2136001;
	private const UK CannotBlacklistWarningKey = (UK)2136002;

	private static ISpriteEntry OnSprite = null!;
	private static ISpriteEntry OffSprite = null!;

	private static readonly Dictionary<Deck, Type?> ExeCache = [];
	private static readonly Dictionary<Type, Deck> ExeTypeToDeck = [];
	private static State? LastState;
	private static double CannotBlacklistWarning = 0;

	public static void ApplyPatches(Harmony harmony)
	{
		OnSprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/ExeOn.png"));
		OffSprite = ModEntry.Instance.Helper.Content.Sprites.RegisterSprite(ModEntry.Instance.Package.PackageRoot.GetRelativeFile("assets/ExeOff.png"));

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
			original: AccessTools.DeclaredMethod(typeof(Character), nameof(Character.Render))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Character)}.{nameof(Character.Render)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Character_Render_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(RunConfig), nameof(RunConfig.IsValid))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(RunConfig)}.{nameof(RunConfig.IsValid)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RunConfig_IsValid_Postfix))
		);
	}

	private static Type? GetExeType(Deck deck)
	{
		if (ExeCache.TryGetValue(deck, out var exeType))
			return exeType;

		if (deck == Deck.colorless)
		{
			ExeCache[deck] = null;
			return null;
		}
		if (ModEntry.Instance.Helper.Content.Characters.LookupByDeck(deck) is { } entry && entry.Configuration.ExeCardType is { } entryExeType)
		{
			ExeCache[deck] = entryExeType;
			ExeTypeToDeck[entryExeType] = deck;
			return entryExeType;
		}

		var fakeShip = Mutil.DeepCopy(StarterShip.ships.Values.First());
		fakeShip.cards.Clear();
		fakeShip.artifacts.Clear();

		try
		{
			var fakeState = Mutil.DeepCopy(DB.fakeState);
			fakeState.slot = null;
			fakeState.PopulateRun(
				shipTemplate: fakeShip,
				chars: NewRunOptions.allChars
					.Where(d => d != deck && d != Deck.dizzy) // need at least 2 characters total, otherwise it will always throw
					.ToHashSet()
			);

			exeType = fakeState.deck
				.Where(card => card is not ColorlessDizzySummon)
				.SingleOrDefault(card => card.GetMeta().deck == Deck.colorless && card.GetFullDisplayName().Contains(".EXE", StringComparison.OrdinalIgnoreCase))?.GetType();
			ExeCache[deck] = exeType;
			if (exeType is not null)
				ExeTypeToDeck[exeType] = deck;
			return exeType;
		}
		catch
		{
			ExeCache[deck] = null;
			return null;
		}
	}

	private static IEnumerable<Deck> GetAllExeCharacters()
		=> NewRunOptions.allChars.Where(d => GetExeType(d) is not null);

	private static IEnumerable<Deck> GetNonBlacklistedExeCharacters(RunConfig runConfig)
		=> GetAllExeCharacters().Where(d => !runConfig.selectedChars.Contains(d)).Where(d => !runConfig.IsBlacklistedExe(d));

	private static void State_PopulateRun_Prefix(State __instance)
		=> LastState = __instance;

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

		for (var i = cards.Count - 1; i >= 0; i--)
			if (ExeTypeToDeck.TryGetValue(cards[i].GetType(), out var exeDeck))
				if (state.runConfig.IsBlacklistedExe(exeDeck))
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
		if (GetNonBlacklistedExeCharacters(g.state.runConfig).Count() >= 2)
			return true;

		CannotBlacklistWarning = 1.5;
		Audio.Play(Event.ZeroEnergy);
		return false;
	}

	private static void Character_Render_Postfix(Character __instance, G g, bool mini, bool? isSelected, bool renderLocked)
	{
		if (isSelected == true)
			return;
		if (!mini || renderLocked)
			return;
		if (g.state.route is not NewRunOptions)
			return;
		if (__instance.deckType is not { } deck)
			return;
		if (deck == Deck.colorless)
			return;
		if (g.state.runConfig.selectedChars.Contains(deck))
			return;
		if (!g.state.runConfig.selectedChars.Contains(Deck.colorless))
			return;
		if (ModEntry.Instance.MoreDifficultiesApi?.AreAltStartersEnabled(g.state, Deck.colorless) == true)
			return;
		if (GetExeType(deck) is not { } exeType)
			return;
		if (g.boxes.FirstOrDefault(b => b.key is { } key && key.k == StableUK.char_mini && key.v == (int)deck) is not { } characterBox)
			return;

		var isBlacklisted = g.state.runConfig.IsBlacklistedExe(deck);

		var box = g.Push(new UIKey(ExeBlacklistKey, (int)deck), new Rect(4, 13, 7, 7), onMouseDown: new MouseDownHandler(() =>
		{
			if (isBlacklisted)
			{
				g.state.runConfig.SetBlacklistedExe(deck, false);
				Audio.Play(Event.Click);
				return;
			}

			if (GetNonBlacklistedExeCharacters(g.state.runConfig).Count() <= 2)
			{
				CannotBlacklistWarning = 1.5;
				Audio.Play(Event.ZeroEnergy);
				return;
			}

			g.state.runConfig.SetBlacklistedExe(deck, true);
			Audio.Play(Event.Click);
		}));
		
		Draw.Sprite(isBlacklisted ? OffSprite.Sprite : OnSprite.Sprite, box.rect.x, box.rect.y, color: DB.decks[Deck.colorless].color);
		if (box.IsHover())
		{
			var tooltipPosition = box.rect.xy + new Vec(32);
			g.tooltips.Add(tooltipPosition, ModEntry.Instance.Localizations.Localize(["exeBlacklist", isBlacklisted ? "off" : "on"]));
			g.tooltips.Add(tooltipPosition, new TTCard { card = (Card)Activator.CreateInstance(exeType)! });
		}

		g.Pop();
	}

	private static void RunConfig_IsValid_Postfix(RunConfig __instance, G g, ref bool __result)
	{
		if (!__instance.selectedChars.Contains(Deck.colorless))
			return;
		if (ModEntry.Instance.MoreDifficultiesApi?.AreAltStartersEnabled(g.state, Deck.colorless) == true)
			return;
		if (!__result)
			return;
		if (GetNonBlacklistedExeCharacters(__instance).Count() < 2)
			__result = false;
	}

	private sealed record MouseDownHandler(Action Delegate) : OnMouseDown
	{
		public void OnMouseDown(G _1, Box _2)
			=> this.Delegate();
	}
}

using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel.Bugfixes;

internal static class CardBrowseOrderFixes
{
	public static void ApplyPatches(IHarmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.GetCardList))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(CardBrowse)}.{nameof(CardBrowse.GetCardList)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Transpiler))
		);

	private static IEnumerable<CodeInstruction> CardBrowse_GetCardList_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			var matcher = new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(new ElementMatch<CodeInstruction>($"{{OpCode: switch, operand: 4 elements}}", i => i.opcode == OpCodes.Switch && i.operand is Label[] { Length: 4 }));
			
			// TODO: make this a Shrike method
			var sortCardsByDeckLabel = ((Label[])matcher.Element().operand)[0];
			var sortCardsByRarityLabel = ((Label[])matcher.Element().operand)[3];
			
			return matcher
				.PointerMatcher(sortCardsByDeckLabel)
				.CreateLdlocInstruction(out var ldlocCards)
				.ExtractLabels(out var labels)
				.EncompassUntil(SequenceMatcherPastBoundsDirection.After, [
					ILMatches.Stloc<IOrderedEnumerable<Card>>(originalMethod).CreateStlocInstruction(out var stlocOrderedCards),
				])
				.Replace([
					ldlocCards.WithLabels(labels),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Transpiler_SortCardsByDeck))),
					stlocOrderedCards,
				])
				
				.PointerMatcher(sortCardsByRarityLabel)
				.CreateLdlocInstruction(out ldlocCards)
				.ExtractLabels(out labels)
				.EncompassUntil(SequenceMatcherPastBoundsDirection.After, [
					ILMatches.Stloc<IOrderedEnumerable<Card>>(originalMethod).CreateStlocInstruction(out stlocOrderedCards),
				])
				.Replace([
					ldlocCards.WithLabels(labels),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Transpiler_SortCardsByRarity))),
					stlocOrderedCards,
				])
				
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}

	private static IOrderedEnumerable<Card> CardBrowse_GetCardList_Transpiler_SortCardsByDeck(List<Card> cards)
		=> cards
			.OrderByDescending(c => c.GetMeta().deck == Deck.colorless)
			.ThenBy(c =>
			{
				var deck = c.GetMeta().deck;
				if (deck == Deck.colorless)
					return -1;
				var index = NewRunOptions.allChars.IndexOf(deck);
				return index == -1 ? int.MaxValue : index;
			})
			.ThenBy(c => c.GetMeta().deck)
			.ThenBy(c => c.GetLocName())
			.ThenBy(c => c.uuid);

	private static IOrderedEnumerable<Card> CardBrowse_GetCardList_Transpiler_SortCardsByRarity(List<Card> cards)
		=> cards
			.OrderBy(c => c.GetMeta().rarity)
			.ThenByDescending(c => c.GetMeta().deck == Deck.colorless)
			.ThenBy(c =>
			{
				var deck = c.GetMeta().deck;
				if (deck == Deck.colorless)
					return -1;
				var index = NewRunOptions.allChars.IndexOf(deck);
				return index == -1 ? int.MaxValue : index;
			})
			.ThenBy(c => c.GetMeta().deck)
			.ThenBy(c => c.GetLocName())
			.ThenBy(c => c.uuid);
}

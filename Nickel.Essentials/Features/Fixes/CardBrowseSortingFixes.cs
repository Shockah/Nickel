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

internal static class CardBrowseSortingFixes
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
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(ILMatches.Ldloc<IOrderedEnumerable<Card>>(originalMethod).CreateLdlocaInstruction(out var ldlocaOrderedCards))
				.ExtractLabels(out var labels)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					new CodeInstruction(OpCodes.Ldarg_1),
					ldlocaOrderedCards,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Transpiler_ModifyOrderedCards))),
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

	private static void CardBrowse_GetCardList_Transpiler_ModifyOrderedCards(CardBrowse route, G g, ref IOrderedEnumerable<Card> orderedCards)
	{
		if (g.state.route is not Combat combat)
			return;
		if (route is { browseSource: CardBrowse.Source.Hand, sortMode: CardBrowse.SortMode.Deck })
			orderedCards = orderedCards.OrderBy(card => combat.hand.IndexOf(card));
	}
}

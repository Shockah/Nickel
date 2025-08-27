using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Nickel.Bugfixes;

internal static class RunSummaryCardOrderFixes
{
	private static RunSummaryRoute? LastRunSummaryRoute;
	
	public static void ApplyPatches(IHarmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(RunSummaryRoute), nameof(RunSummaryRoute.Render))
					?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(RunSummaryRoute)}.{nameof(RunSummaryRoute.Render)}`"),
			prefix: new HarmonyMethod(AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RunSummaryRoute_Render_Prefix_First)), priority: Priority.First),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(RunSummaryRoute_Render_Transpiler))
		);

	private static void RunSummaryRoute_Render_Prefix_First(RunSummaryRoute __instance)
	{
		if (__instance.runSummary is not { } runSummary)
			return;
		if (LastRunSummaryRoute == __instance)
			return;
		
		LastRunSummaryRoute = __instance;
		runSummary.cards = runSummary.cards
			.Select(cardSummary => (Summary: cardSummary, Type: DB.cards.GetValueOrDefault(cardSummary.type), Meta: DB.cardMetas.GetValueOrDefault(cardSummary.type)))
			.Where(e => e.Type is not null && e.Meta is not null)
			.Select(e =>
			{
				ref var card = ref CollectionsMarshal.GetValueRefOrAddDefault(__instance.cardCache, e.Summary.type, out var cardExists);
				if (!cardExists)
					card = (Card)Activator.CreateInstance(e.Type!)!;
				return (Summary: e.Summary, Card: card!, Meta: e.Meta!);
			})
			.OrderByDescending(e => e.Meta.deck == Deck.colorless || runSummary.decks.Contains(e.Meta.deck))
			.ThenBy(e =>
			{
				if (e.Meta.deck == Deck.colorless)
					return -1;
				var index = NewRunOptions.allChars.IndexOf(e.Meta.deck);
				return index == -1 ? int.MaxValue : index;
			})
			.ThenBy(e => e.Meta.deck)
			.ThenBy(e => e.Card.GetLocName())
			.ThenBy(e => e.Summary.upgrade)
			.Select(e => e.Summary)
			.ToList();
	}
	
	private static IEnumerable<CodeInstruction> RunSummaryRoute_Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<List<Card>>(originalMethod).ExtractLabels(out var labels),
					ILMatches.Instruction(OpCodes.Ldsfld),
					ILMatches.Instruction(OpCodes.Dup),
				])
				.EncompassUntil(SequenceMatcherPastBoundsDirection.After, ILMatches.Stloc<List<Card>>(originalMethod))
				.EncompassUntil(SequenceMatcherPastBoundsDirection.After, [
					ILMatches.Ldloc<List<Card>>(originalMethod),
					ILMatches.Instruction(OpCodes.Ldsfld),
					ILMatches.Instruction(OpCodes.Dup),
				])
				.EncompassUntil(SequenceMatcherPastBoundsDirection.After, ILMatches.Stloc<List<Card>>(originalMethod))
				.Remove()
				.PointerMatcher(SequenceMatcherRelativeElement.AfterLast)
				.AddLabels(labels)
				.AllElements();
		}
		catch (Exception ex)
		{
			ModEntry.Instance.Logger.LogError("Could not patch method {DeclaringType}::{Method} - {Mod} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, ModEntry.Instance.Package.Manifest.UniqueName, ex);
			return instructions;
		}
		// ReSharper restore PossibleMultipleEnumeration
	}
}

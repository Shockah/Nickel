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

internal static class CardBrowseOrderSortMode
{
	private static readonly CardBrowse.SortMode OrderSortMode = ModEntry.Instance.Helper.Utilities.ObtainEnumCase<CardBrowse.SortMode>();
	private static readonly Pool<Args> ArgsPool = new(() => new());
	private static bool ModifiedEnabledSortModes;

	public static void ApplyPatches(IHarmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.GetCardList))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(CardBrowse)}.{nameof(CardBrowse.GetCardList)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.Render))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(CardBrowse)}.{nameof(CardBrowse.Render)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_Render_Prefix)),
			finalizer: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_Render_Finalizer))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(CardBrowse), nameof(CardBrowse.GetSortModeLabel))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(CardBrowse)}.{nameof(CardBrowse.GetSortModeLabel)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetSortModeLabel_Postfix))
		);
	}

	private static IEnumerable<CodeInstruction> CardBrowse_GetCardList_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod, ILGenerator il)
	{
		// ReSharper disable PossibleMultipleEnumeration
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldloc<List<Card>>(originalMethod).CreateLdlocInstruction(out var ldlocCards),
					ILMatches.AnyLdloc,
					ILMatches.Instruction(OpCodes.Ldftn),
					ILMatches.Instruction(OpCodes.Newobj),
					ILMatches.Call("RemoveAll")
				])
				.Find([
					ILMatches.Ldloc<CardBrowse.SortMode>(originalMethod).CreateLdlocInstruction(out var ldlocSortMode).ExtractLabels(out var defaultBranchLabels),
					ILMatches.Instruction(OpCodes.Box),
					ILMatches.Call("ThrowSwitchExpressionException"),
					ILMatches.Ldloc<IOrderedEnumerable<Card>>(originalMethod).CreateLdlocaInstruction(out var ldlocaOrderedCards).CreateLabel(il, out var successLabel),
				])
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					ldlocSortMode.Value.WithLabels(defaultBranchLabels),
					ldlocCards,
					ldlocaOrderedCards,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CardBrowse_GetCardList_Transpiler_HandleOrderedSortMode))),
					new CodeInstruction(OpCodes.Brtrue, successLabel),
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

	private static bool CardBrowse_GetCardList_Transpiler_HandleOrderedSortMode(CardBrowse.SortMode sortMode, List<Card> cards, ref IOrderedEnumerable<Card> orderedCards)
	{
		if (sortMode != OrderSortMode)
			return false;
		
		orderedCards = cards.OrderBy(cards.IndexOf);
		return true;
	}

	private static void CardBrowse_Render_Prefix(CardBrowse __instance, G g)
	{
		if (!ShouldAllowOrderSortMode())
			return;
		
		ModifiedEnabledSortModes = true;
		CardBrowse.enabledSortModes.Insert(0, OrderSortMode);

		if (ModEntry.Instance.Helper.ModData.GetModDataOrDefault<bool>(__instance, "SwitchedToOrderSortModeOnce"))
			return;
		
		ModEntry.Instance.Helper.ModData.SetModData(__instance, "SwitchedToOrderSortModeOnce", true);
		__instance.sortMode = OrderSortMode;

		bool ShouldAllowOrderSortMode()
			=> ArgsPool.Do(args =>
			{
				args.Route = __instance;
				
				foreach (var hook in ModEntry.Instance.Hooks.GetHooksWithProxies(ModEntry.Instance.Helper.Utilities.ProxyManager, g.state.EnumerateAllArtifacts()))
					if (hook.ShouldAllowOrderSortModeInCardBrowse(args) is { } result)
						return result;
				return false;
			});
	}

	private static void CardBrowse_Render_Finalizer()
	{
		if (!ModifiedEnabledSortModes)
			return;

		ModifiedEnabledSortModes = false;
		CardBrowse.enabledSortModes.Remove(OrderSortMode);
	}

	private static void CardBrowse_GetSortModeLabel_Postfix(CardBrowse.SortMode mode, ref string __result)
	{
		if (mode == OrderSortMode)
			__result = ModEntry.Instance.Localizations.Localize(["cardBrowseOrderSortMode", "mode"]);
	}

	internal sealed class Hook : IEssentialsApi.IHook
	{
		public bool? ShouldAllowOrderSortModeInCardBrowse(IEssentialsApi.IHook.IShouldAllowOrderSortModeInCardBrowseArgs args)
			=> args.Route.browseSource is CardBrowse.Source.Hand or CardBrowse.Source.DiscardPile or CardBrowse.Source.ExhaustPile ? true : null;
	}

	private sealed class Args : IEssentialsApi.IHook.IShouldAllowOrderSortModeInCardBrowseArgs
	{
		public CardBrowse Route { get; set; } = null!;
	}
}

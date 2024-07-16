using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class CardPatches
{
	internal static EventHandler<KeyEventArgs>? OnKey;
	internal static EventHandler<TooltipsEventArgs>? OnGetTooltips;
	internal static EventHandler<TraitRenderEventArgs>? OnRenderTraits;
	internal static EventHandler<GettingDataWithOverridesEventArgs>? OnGettingDataWithOverrides;
	internal static EventHandler<MidGetDataWithOverridesEventArgs>? OnMidGetDataWithOverrides;
	
	private static readonly KeyEventArgs KeyEventArgsInstance = new();
	private static readonly TooltipsEventArgs TooltipsEventArgsInstance = new();
	private static readonly TraitRenderEventArgs TraitRenderEventArgsInstance = new();
	private static readonly GettingDataWithOverridesEventArgs GettingDataWithOverridesEventArgsInstance = new();
	private static readonly MidGetDataWithOverridesEventArgs MidGetDataWithOverridesEventArgsInstance = new();

	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Key))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Card)}.{nameof(Card.Key)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Key_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Card)}.{nameof(Card.Render)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Render_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetAllTooltips))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Card)}.{nameof(Card.GetAllTooltips)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetAllTooltips_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetDataWithOverrides))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Card)}.{nameof(Card.GetDataWithOverrides)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetDataWithOverrides_Prefix)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetDataWithOverrides_Transpiler))
		);
	}

	private static void Key_Postfix(Card __instance, ref string __result)
	{
		var args = KeyEventArgsInstance;
		args.Card = __instance;
		args.Key = __result;
		OnKey?.Invoke(null, args);
		__result = args.Key;
	}

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldloc<State>(originalMethod).CreateLdlocInstruction(out var ldlocState),
					ILMatches.Call("GetDataWithOverrides")
				])
				.Find([
					ILMatches.Ldloc<CardData>(originalMethod).ExtractLabels(out var labels).Anchor(out var findAnchor),
					ILMatches.Ldfld("buoyant"),
					ILMatches.Brfalse
				])
				.Find([
					ILMatches.Ldloc<Vec>(originalMethod).CreateLdlocInstruction(out var ldlocVec),
					ILMatches.Ldfld("y"),
					ILMatches.LdcI4(8),
					ILMatches.Ldloc<int>(originalMethod).CreateLdlocaInstruction(out var ldlocaCardTraitIndex),
					ILMatches.Instruction(OpCodes.Dup),
					ILMatches.LdcI4(1),
					ILMatches.Instruction(OpCodes.Add),
					ILMatches.Stloc<int>(originalMethod)
				])
				.Anchors().PointerMatcher(findAnchor)
				.Insert(SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					ldlocState,
					ldlocaCardTraitIndex,
					ldlocVec,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Render_Transpiler_RenderTraits)))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			Nickel.Instance.ModManager.Logger.LogCritical("Could not patch method {Method} - {ModLoaderName} probably won't work.\nReason: {Exception}", originalMethod, NickelConstants.Name, ex);
			return instructions;
		}
	}

	private static void Render_Transpiler_RenderTraits(Card card, State state, ref int cardTraitIndex, Vec vec)
	{
		var args = TraitRenderEventArgsInstance;
		args.Card = card;
		args.State = state;
		args.CardTraitIndex = cardTraitIndex;
		args.Position = vec;
		OnRenderTraits?.Invoke(null, args);
		cardTraitIndex = args.CardTraitIndex;
	}

	private static void GetAllTooltips_Postfix(Card __instance, State s, bool showCardTraits, ref IEnumerable<Tooltip> __result)
	{
		var args = TooltipsEventArgsInstance;
		args.Card = __instance;
		args.State = s;
		args.ShowCardTraits = showCardTraits;
		args.TooltipsEnumerator = __result;
		OnGetTooltips?.Invoke(null, args);
		__result = args.TooltipsEnumerator;
	}

	private static void GetDataWithOverrides_Prefix(Card __instance, State state)
	{
		var args = GettingDataWithOverridesEventArgsInstance;
		args.Card = __instance;
		args.State = state;
		OnGettingDataWithOverrides?.Invoke(null, args);
	}

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> GetDataWithOverrides_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldarg(1),
					ILMatches.Call("GetData"),
					ILMatches.Stloc<CardData>(originalMethod).CreateLdlocaInstruction(out var ldlocaCardData)
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Ldarg_1),
					ldlocaCardData,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetDataWithOverrides_Transpiler_ModifyData)))
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			Nickel.Instance.ModManager.Logger.LogCritical("Could not patch method {Method} - {ModLoaderName} probably won't work.\nReason: {Exception}", originalMethod, NickelConstants.Name, ex);
			return instructions;
		}
	}

	private static void GetDataWithOverrides_Transpiler_ModifyData(Card card, State state, ref CardData data)
	{
		var args = MidGetDataWithOverridesEventArgsInstance;
		args.Card = card;
		args.State = state;
		args.InitialData = data;
		args.CurrentData = data;
		OnMidGetDataWithOverrides?.Invoke(null, args);
		data = args.CurrentData;
	}

	internal sealed class KeyEventArgs
	{
		public Card Card { get; internal set; } = null!;
		public string Key { get; set; } = null!;
	}

	internal sealed class TooltipsEventArgs
	{
		public Card Card { get; internal set; } = null!;
		public State State { get; internal set; } = null!;
		public bool ShowCardTraits { get; internal set; }
		public IEnumerable<Tooltip> TooltipsEnumerator { get; set; } = null!;
	}

	internal sealed class TraitRenderEventArgs
	{
		public Card Card { get; internal set; } = null!;
		public State State { get; internal set; } = null!;
		public int CardTraitIndex { get; set; }
		public Vec Position { get; set; }
	}

	internal sealed class GettingDataWithOverridesEventArgs
	{
		public Card Card { get; internal set; } = null!;
		public State State { get; internal set; } = null!;
	}

	internal sealed class MidGetDataWithOverridesEventArgs
	{
		public Card Card { get; internal set; } = null!;
		public State State { get; internal set; } = null!;
		public CardData InitialData { get; internal set; }
		public CardData CurrentData;
	}
}

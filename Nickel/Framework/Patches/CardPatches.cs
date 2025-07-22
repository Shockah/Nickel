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

internal static class CardPatches
{
	internal static RefEventHandler<KeyEventArgs>? OnKey;
	internal static RefEventHandler<TooltipsEventArgs>? OnGetTooltips;
	internal static RefEventHandler<ModifyShineColorEventArgs>? OnModifyShineColor;
	internal static RefEventHandler<TraitRenderEventArgs>? OnRenderTraits;
	internal static EventHandler<GettingDataWithOverridesEventArgs>? OnGettingDataWithOverrides;
	internal static RefEventHandler<MidGetDataWithOverridesEventArgs>? OnMidGetDataWithOverrides;
	internal static EventHandler<Card>? OnCopyingWithNewId;

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
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.CopyWithNewId))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Card)}.{nameof(Card.CopyWithNewId)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(CopyWithNewId_Prefix))
		);
	}

	private static void Key_Postfix(Card __instance, ref string __result)
	{
		var args = new KeyEventArgs
		{
			Card = __instance,
			Key = __result,
		};
		OnKey?.Invoke(null, ref args);
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
					ILMatches.Call("GetDataWithOverrides"),
				])
				.Find([
					ILMatches.Ldloc<CardMeta>(originalMethod),
					ILMatches.Ldfld("deck"),
					ILMatches.Stloc<Deck>(originalMethod),
					ILMatches.Ldloc<Deck>(originalMethod),
					ILMatches.Instruction(OpCodes.Switch),
				])
				.Find([
					ILMatches.Ldflda("color"),
					ILMatches.Call("normalize"),
					ILMatches.Stloc<Color>(originalMethod),
					ILMatches.Ldloca<Color>(originalMethod),
					ILMatches.Ldloc<double>(originalMethod),
					ILMatches.Call("gain"),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					ldlocState,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Render_Transpiler_ModifyShineColor))),
				])
				.Find([
					ILMatches.Ldflda("color"),
					ILMatches.Call("normalize"),
					ILMatches.Stloc<Color>(originalMethod),
					ILMatches.Ldloca<Color>(originalMethod),
					ILMatches.Ldloc<double>(originalMethod),
					ILMatches.Call("gain"),
				])
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					ldlocState,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Render_Transpiler_ModifyShineColor))),
				])
				.Find([
					ILMatches.Ldloc<CardData>(originalMethod).ExtractLabels(out var labels).Anchor(out var findAnchor),
					ILMatches.Ldfld("buoyant"),
					ILMatches.Brfalse,
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
			Nickel.Instance.ModManager.Logger.LogCritical("Could not patch method {DeclaringType}::{Method} - {ModLoaderName} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, NickelConstants.Name, ex);
			return instructions;
		}
	}

	private static Color Render_Transpiler_ModifyShineColor(Color shineColor, Card card, State state)
	{
		var args = new ModifyShineColorEventArgs
		{
			Card = card,
			State = state,
			ShineColor = shineColor,
		};
		OnModifyShineColor?.Invoke(null, ref args);
		return args.ShineColor;
	}

	private static void Render_Transpiler_RenderTraits(Card card, State state, ref int cardTraitIndex, Vec vec)
	{
		var args = new TraitRenderEventArgs
		{
			Card = card,
			State = state,
			CardTraitIndex = cardTraitIndex,
			Position = vec,
		};
		OnRenderTraits?.Invoke(null, ref args);
		cardTraitIndex = args.CardTraitIndex;
	}

	private static void GetAllTooltips_Postfix(Card __instance, State s, bool showCardTraits, ref IEnumerable<Tooltip> __result)
	{
		var args = new TooltipsEventArgs
		{
			Card = __instance,
			State = s,
			ShowCardTraits = showCardTraits,
			TooltipsEnumerator = __result,
		};
		OnGetTooltips?.Invoke(null, ref args);
		__result = args.TooltipsEnumerator;
	}

	private static void GetDataWithOverrides_Prefix(Card __instance, State state)
		=> OnGettingDataWithOverrides?.Invoke(null, new GettingDataWithOverridesEventArgs
		{
			Card = __instance,
			State = state,
		});

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
			Nickel.Instance.ModManager.Logger.LogCritical("Could not patch method {DeclaringType}::{Method} - {ModLoaderName} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, NickelConstants.Name, ex);
			return instructions;
		}
	}

	private static void GetDataWithOverrides_Transpiler_ModifyData(Card card, State state, ref CardData data)
	{
		var args = new MidGetDataWithOverridesEventArgs
		{
			Card = card,
			State = state,
			InitialData = data,
			CurrentData = data,
		};
		OnMidGetDataWithOverrides?.Invoke(null, ref args);
		data = args.CurrentData;
	}

	private static void CopyWithNewId_Prefix(Card __instance)
		=> OnCopyingWithNewId?.Invoke(null, __instance);

	internal struct KeyEventArgs
	{
		public required Card Card { get; init; }
		public required string Key;
	}

	internal struct TooltipsEventArgs
	{
		public required Card Card { get; init; }
		public required State State { get; init; }
		public required bool ShowCardTraits { get; init; }
		public required IEnumerable<Tooltip> TooltipsEnumerator;
	}

	internal struct ModifyShineColorEventArgs
	{
		public required Card Card { get; init; }
		public required State State { get; init; }
		public required Color ShineColor;
	}

	internal struct TraitRenderEventArgs
	{
		public required Card Card { get; init; }
		public required State State { get; init; }
		public required int CardTraitIndex;
		public required Vec Position;
	}

	internal readonly struct GettingDataWithOverridesEventArgs
	{
		public required Card Card { get; init; }
		public required State State { get; init; }
	}

	internal struct MidGetDataWithOverridesEventArgs
	{
		public required Card Card { get; init; }
		public required State State { get; init; }
		public required CardData InitialData { get; init; }
		public required CardData CurrentData;
	}
}

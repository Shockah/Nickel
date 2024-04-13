using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using WeakEvent;

namespace Nickel;

internal static class CardPatches
{
	internal static readonly WeakEventSource<KeyEventArgs> OnKey = new();
	internal static readonly WeakEventSource<TooltipsEventArgs> OnGetTooltips = new();
	internal static readonly WeakEventSource<TraitRenderEventArgs> OnRenderTraits = new();
	internal static readonly WeakEventSource<GettingDataWithOverridesEventArgs> OnGettingDataWithOverrides = new();
	internal static readonly WeakEventSource<GetDataWithOverridesEventArgs> OnGetDataWithOverrides = new();

	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Key))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Card)}.{nameof(Card.Key)}`"),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Key_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.Render)),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Render_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetAllTooltips)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetAllTooltips_Postfix))
		);
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Card), nameof(Card.GetDataWithOverrides)),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetDataWithOverrides_Prefix)),
			postfix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(GetDataWithOverrides_Postfix))
		);
	}

	private static void Key_Postfix(Card __instance, ref string __result)
	{
		var eventArgs = new KeyEventArgs { Card = __instance, Key = __result };
		OnKey.Raise(null, eventArgs);
		__result = eventArgs.Key;
	}

	private static IEnumerable<CodeInstruction> Render_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find(
					ILMatches.Ldarg(0),
					ILMatches.Ldloc<State>(originalMethod).CreateLdlocInstruction(out var ldlocState),
					ILMatches.Call("GetDataWithOverrides")
				)
				.Find(
					ILMatches.Ldloc<CardData>(originalMethod).ExtractLabels(out var labels).Anchor(out var findAnchor),
					ILMatches.Ldfld("buoyant"),
					ILMatches.Brfalse
				)
				.Find(
					ILMatches.Ldloc<Vec>(originalMethod).CreateLdlocInstruction(out var ldlocVec),
					ILMatches.Ldfld("y"),
					ILMatches.LdcI4(8),
					ILMatches.Ldloc<int>(originalMethod).CreateLdlocaInstruction(out var ldlocaCardTraitIndex),
					ILMatches.Instruction(OpCodes.Dup),
					ILMatches.LdcI4(1),
					ILMatches.Instruction(OpCodes.Add),
					ILMatches.Stloc<int>(originalMethod)
				)
				.Anchors().PointerMatcher(findAnchor)
				.Insert(
					SequenceMatcherPastBoundsDirection.Before, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Ldarg_0).WithLabels(labels),
					ldlocState,
					ldlocaCardTraitIndex,
					ldlocVec,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(CardPatches), nameof(Render_Transpiler_RenderTraits)))
				)
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
		var eventArgs = new TraitRenderEventArgs { Card = card, State = state, CardTraitIndex = cardTraitIndex, Position = vec };
		OnRenderTraits.Raise(null, eventArgs);
	}

	private static void GetAllTooltips_Postfix(Card __instance, State s, bool showCardTraits, ref IEnumerable<Tooltip> __result)
	{
		var eventArgs = new TooltipsEventArgs { Card = __instance, State = s, ShowCardTraits = showCardTraits, TooltipsEnumerator = __result };
		OnGetTooltips.Raise(null, eventArgs);
		__result = eventArgs.TooltipsEnumerator;
	}

	private static void GetDataWithOverrides_Prefix(Card __instance, State state)
	{
		var eventArgs = new GettingDataWithOverridesEventArgs { Card = __instance, State = state };
		OnGettingDataWithOverrides.Raise(null, eventArgs);
	}

	private static void GetDataWithOverrides_Postfix(Card __instance, State state, ref CardData __result)
	{
		var eventArgs = new GetDataWithOverridesEventArgs { Card = __instance, State = state, Data = __result };
		OnGetDataWithOverrides.Raise(null, eventArgs);
		__result = eventArgs.Data;
	}

	internal sealed class KeyEventArgs
	{
		public required Card Card { get; init; }
		public required string Key { get; set; }
	}

	internal sealed class TooltipsEventArgs
	{
		public required Card Card { get; init; }
		public required State State { get; init; }
		public required bool ShowCardTraits { get; init; }
		public required IEnumerable<Tooltip> TooltipsEnumerator { get; set; }
	}

	internal sealed class TraitRenderEventArgs
	{
		public required Card Card { get; init; }
		public required State State { get; init; }
		public required int CardTraitIndex { get; set; }
		public required Vec Position { get; set; }
	}

	internal sealed class GettingDataWithOverridesEventArgs
	{
		public required Card Card { get; init; }
		public required State State { get; init; }
	}

	internal sealed class GetDataWithOverridesEventArgs
	{
		public required Card Card { get; init; }
		public required State State { get; init; }
		public required CardData Data;
	}
}

using FMOD;
using FMOD.Studio;
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

internal static class ShoutPatches
{
	internal static RefEventHandler<ModifyBabblePeriodEventArgs>? OnModifyBabblePeriod;
	internal static RefEventHandler<ModifyBabbleSoundEventArgs>? OnModifyBabbleSound;

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Shout), nameof(Shout.Update))
				?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Shout)}.{nameof(Shout.Update)}`"),
			transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Update_Transpiler))
		);

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("progress"),
					ILMatches.Ldarg(0),
					ILMatches.Ldfld("lastBabble"),
					ILMatches.LdcR8(Shout.BABBLE_INTERVAL_LETTERS).Anchor(out var babblePeriodAnchor),
					ILMatches.Instruction(OpCodes.Add),
					ILMatches.AnyBle,
				])
				.Anchors().PointerMatcher(babblePeriodAnchor)
				.Insert(SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion, [
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Update_Transpiler_ModifyBabblePeriod))),
				])
				.Find([
					ILMatches.Ldloc<string>(originalMethod),
					ILMatches.Call("GetCharBabble"),
					ILMatches.Newobj(AccessTools.DeclaredConstructor(typeof(GUID?), [typeof(GUID)])),
					ILMatches.LdcI4(1),
					ILMatches.Call("Play"),
				])
				.PointerMatcher(SequenceMatcherRelativeElement.Last)
				.Replace([
					new CodeInstruction(OpCodes.Ldarg_0),
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Update_Transpiler_DoPlay))),
				])
				.AllElements();
		}
		catch (Exception ex)
		{
			Nickel.Instance.ModManager.Logger.LogCritical("Could not patch method {DeclaringType}::{Method} - {ModLoaderName} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, NickelConstants.Name, ex);
			return instructions;
		}
	}

	private static double Update_Transpiler_ModifyBabblePeriod(double babblePeriod, Shout shout)
	{
		var args = new ModifyBabblePeriodEventArgs
		{
			Shout = shout,
			Period = babblePeriod,
		};
		OnModifyBabblePeriod?.Invoke(null, ref args);
		return args.Period;
	}

	private static EventInstance? Update_Transpiler_DoPlay(GUID? babbleId, bool release, Shout shout)
	{
		var args = new ModifyBabbleSoundEventArgs
		{
			Shout = shout,
			Sound = null,
		};
		OnModifyBabbleSound?.Invoke(null, ref args);
		
		if (args.Sound is null)
			return Audio.Play(babbleId, release);
		
		var instance = args.Sound.CreateInstance();
		return instance is IEventSoundInstance eventInstance ? eventInstance.Instance : null;
	}

	internal struct ModifyBabblePeriodEventArgs
	{
		public required Shout Shout { get; init; }
		public required double Period;
	}

	internal struct ModifyBabbleSoundEventArgs
	{
		public required Shout Shout { get; init; }
		public required ISoundEntry? Sound;
	}
}

﻿using FMOD;
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
	internal static EventHandler<ModifyBabblePeriodEventArgs>? OnModifyBabblePeriod;
	internal static EventHandler<ModifyBabbleSoundEventArgs>? OnModifyBabbleSound;
	
	private static readonly Pool<ModifyBabblePeriodEventArgs> ModifyBabblePeriodEventArgsPool = new(() => new());
	private static readonly Pool<ModifyBabbleSoundEventArgs> ModifyBabbleSoundEventArgsPool = new(() => new());

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
			Nickel.Instance.ModManager.Logger.LogCritical("Could not patch method {Method} - {ModLoaderName} probably won't work.\nReason: {Exception}", originalMethod, NickelConstants.Name, ex);
			return instructions;
		}
	}

	private static double Update_Transpiler_ModifyBabblePeriod(double babblePeriod, Shout shout)
		=> ModifyBabblePeriodEventArgsPool.Do(args =>
		{
			args.Shout = shout;
			args.Period = babblePeriod;
			OnModifyBabblePeriod?.Invoke(null, args);
			return args.Period;
		});

	private static EventInstance? Update_Transpiler_DoPlay(GUID? babbleId, bool release, Shout shout)
	{
		var sound = ModifyBabbleSoundEventArgsPool.Do(args =>
		{
			args.Shout = shout;
			args.Sound = null;
			OnModifyBabbleSound?.Invoke(null, args);
			return args.Sound;
		});

		if (sound is null)
			return Audio.Play(babbleId, release);

		var instance = sound.CreateInstance();
		return instance is IEventSoundInstance eventInstance ? eventInstance.Instance : null;
	}

	internal sealed class ModifyBabblePeriodEventArgs
	{
		public Shout Shout { get; internal set; } = null!;
		public double Period { get; set; }
	}

	internal sealed class ModifyBabbleSoundEventArgs
	{
		public Shout Shout { get; internal set; } = null!;
		public ISoundEntry? Sound { get; set; }
	}
}
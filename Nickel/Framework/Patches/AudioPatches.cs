using FMOD;
using HarmonyLib;
using Microsoft.Extensions.Logging;
using Nanoray.Shrike;
using Nanoray.Shrike.Harmony;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Nickel;

internal static class AudioPatches
{
	internal static RefEventHandler<PlaySongArgs>? OnPlaySong;

	internal static void Apply(Harmony harmony)
	{
		harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Audio), nameof(Audio.PlaySong))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Audio)}.{nameof(Audio.PlaySong)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(PlaySong_Prefix))
		);

		if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			harmony.Patch(
				original: AccessTools.DeclaredConstructor(typeof(Audio), [])
				          ?? throw new InvalidOperationException($"Could not patch game methods: missing ctor `{nameof(Audio)}`"),
				transpiler: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Mac_Ctor_Transpiler))
			);
	}

	private static void PlaySong_Prefix(Audio __instance, ref GUID id, ref MusicState ms)
	{
		var args = new PlaySongArgs
		{
			Audio = __instance,
			Id = id,
			MusicState = ms,
		};
		OnPlaySong?.Invoke(null, ref args);
		id = args.Id;
		ms = args.MusicState;
	}

	[SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
	private static IEnumerable<CodeInstruction> Mac_Ctor_Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase originalMethod)
	{
		try
		{
			return new SequenceBlockMatcher<CodeInstruction>(instructions)
				.Find([
					ILMatches.Call("get_BaseDirectory"),
					ILMatches.Call("GetDirectoryName"),
				])
				.Insert(
					SequenceMatcherPastBoundsDirection.After, SequenceMatcherInsertionResultingBounds.IncludingInsertion,
					new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(Mac_Ctor_Transpiler_ModifyLibPath)))
				)
				.AllElements();
		}
		catch (Exception ex)
		{
			Nickel.Instance.ModManager.Logger.LogCritical("Could not patch method {DeclaringType}::{Method} - {ModLoaderName} probably won't work.\nReason: {Exception}", originalMethod.DeclaringType, originalMethod, NickelConstants.Name, ex);
			return instructions;
		}
	}

	private static string Mac_Ctor_Transpiler_ModifyLibPath(string _)
		=> Directory.GetCurrentDirectory();

	internal struct PlaySongArgs
	{
		public required Audio Audio { get; init; }
		public required GUID Id;
		public required MusicState MusicState;
	}
}

using FMOD;
using HarmonyLib;
using System;
using System.Reflection;

namespace Nickel;

internal static class AudioPatches
{
	internal static RefEventHandler<PlaySongArgs>? OnPlaySong;

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Audio), nameof(Audio.PlaySong))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Audio)}.{nameof(Audio.PlaySong)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(PlaySong_Prefix))
		);

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

	internal struct PlaySongArgs
	{
		public required Audio Audio { get; init; }
		public required GUID Id;
		public required MusicState MusicState;
	}
}

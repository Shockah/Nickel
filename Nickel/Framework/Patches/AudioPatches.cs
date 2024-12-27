using FMOD;
using HarmonyLib;
using System;
using System.Reflection;

namespace Nickel;

internal static class AudioPatches
{
	internal static EventHandler<PlaySongArgs>? OnPlaySong;

	private static readonly Pool<PlaySongArgs> PlaySongArgsPool = new(() => new());

	internal static void Apply(Harmony harmony)
		=> harmony.Patch(
			original: AccessTools.DeclaredMethod(typeof(Audio), nameof(Audio.PlaySong))
			          ?? throw new InvalidOperationException($"Could not patch game methods: missing method `{nameof(Audio)}.{nameof(Audio.PlaySong)}`"),
			prefix: new HarmonyMethod(MethodBase.GetCurrentMethod()!.DeclaringType!, nameof(PlaySong_Prefix))
		);

	private static void PlaySong_Prefix(Audio __instance, ref GUID id, ref MusicState ms)
	{
		var nonRefId = id;
		var nonRefMusicState = ms;
		
		PlaySongArgsPool.Do(args =>
		{
			args.Audio = __instance;
			args.Id = nonRefId;
			args.MusicState = nonRefMusicState;
			OnPlaySong?.Invoke(null, args);
			nonRefId = args.Id;
			nonRefMusicState = args.MusicState;
		});

		id = nonRefId;
		ms = nonRefMusicState;
	}

	internal sealed class PlaySongArgs
	{
		public Audio Audio { get; internal set; } = null!;
		public GUID Id { get; set; }
		public MusicState MusicState { get; set; }
	}
}

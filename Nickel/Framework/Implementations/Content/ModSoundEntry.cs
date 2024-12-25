using System;
using System.IO;

namespace Nickel;

internal sealed class ModSoundEntry(IModManifest modOwner, string uniqueName, string localName, Func<Stream> streamProvider) : IModSoundEntry
{
	public IModManifest ModOwner { get; } = modOwner;
	public string UniqueName { get; } = uniqueName;
	public string LocalName { get; } = localName;

	public FMOD.Sound Sound
		=> this.SoundStorage ?? throw new NullReferenceException("Mod sound entry is not injected yet");
	
	internal Func<Stream>? StreamProvider = streamProvider;
	internal FMOD.Sound? SoundStorage;

	public IModSoundInstance CreateInstance(IModAudio helper, bool started = true)
	{
		if (Audio.inst is not { } audio)
			throw new NullReferenceException($"{nameof(Audio)}.{nameof(Audio.inst)} is `null`");

		Audio.Catch(audio.fmodStudioSystem.getCoreSystem(out var coreSystem));
		Audio.Catch(audio.fmodStudioSystem.getBus("bus:/Sfx", out var bus));
		Audio.Catch(bus.getChannelGroup(out var channelGroup));
		Audio.Catch(coreSystem.playSound(this.Sound, channelGroup, !started, out var channel));
		return new ModSoundInstance(this, channel);
	}
}

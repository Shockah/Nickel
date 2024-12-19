using System;

namespace Nickel;

internal sealed class ModSoundEntry(IModManifest modOwner, string uniqueName, string localName, byte[] data) : ISoundEntry
{
	public IModManifest ModOwner { get; } = modOwner;
	public string UniqueName { get; } = uniqueName;
	public string LocalName { get; } = localName;
	internal byte[]? Data = data;
	internal FMOD.Sound? Sound;

	public ISoundInstance CreateInstance(IModAudio helper, bool started = true)
	{
		if (this.Sound is null && this.Data is null)
			throw new NullReferenceException("Mod sound entry has no data to work with");
		if (Audio.inst is not { } audio)
			throw new NullReferenceException($"{nameof(Audio)}.{nameof(Audio.inst)} is `null`");
		if (this.Sound is null && this.Data is not null)
			throw new InvalidOperationException("Mod sound entry has data, but it has not been loaded yet");

		Audio.Catch(audio.fmodStudioSystem.getCoreSystem(out var coreSystem));
		Audio.Catch(audio.fmodStudioSystem.getBus("bus:/Sfx", out var bus));
		Audio.Catch(bus.getChannelGroup(out var channelGroup));
		Audio.Catch(coreSystem.playSound(this.Sound!.Value, channelGroup, !started, out var channel));
		return new ModSoundInstance(this, channel);
	}
}

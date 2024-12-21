using FMOD;
using Nanoray.PluginManager;
using System;

namespace Nickel;

internal sealed class ModAudio(
	IPluginPackage<IModManifest> package,
	Func<AudioManager> audioManagerProvider
) : IModAudio
{
	public IEventSoundEntry? LookupSoundByEventId(GUID eventId)
		=> audioManagerProvider().LookupSoundByEventId(eventId);

	public ISoundEntry? LookupSoundByUniqueName(string uniqueName)
		=> audioManagerProvider().LookupSoundByUniqueName(uniqueName);
	
	public IModSoundEntry RegisterSound(IFileInfo file)
	{
		string soundName;
		try
		{
			soundName = package.PackageRoot.IsInSameFileSystemType(file)
				? package.PackageRoot.GetRelativePathTo(file).Replace('\\', '/')
				: file.FullName;
		}
		catch
		{
			soundName = file.FullName;
		}
		return audioManagerProvider().RegisterSound(package.Manifest, soundName, file.ReadAllBytes());
	}

	public IModSoundEntry RegisterSound(string name, IFileInfo file)
		=> audioManagerProvider().RegisterSound(package.Manifest, name, file.ReadAllBytes());

	public IModSoundEntry RegisterSound(byte[] data)
		=> audioManagerProvider().RegisterSound(package.Manifest, Guid.NewGuid().ToString(), data);

	public IModSoundEntry RegisterSound(string name, byte[] data)
		=> audioManagerProvider().RegisterSound(package.Manifest, name, data);

	public void RegisterBank(byte[] data)
		=> audioManagerProvider().RegisterBank(data);

	public ISoundInstance CreateInstance(ISoundEntry entry, bool started = true)
		=> entry.CreateInstance(this, started);
}

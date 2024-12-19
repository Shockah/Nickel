using FMOD;
using Nanoray.PluginManager;
using System;

namespace Nickel;

internal sealed class ModAudio(
	IPluginPackage<IModManifest> package,
	Func<AudioManager> audioManagerProvider
) : IModAudio
{
	public ISoundEntry? LookupSoundById(GUID id)
		=> audioManagerProvider().LookupSoundById(id);

	public ISoundEntry? LookupSoundByUniqueName(string uniqueName)
		=> audioManagerProvider().LookupSoundByUniqueName(uniqueName);
	
	public ISoundEntry RegisterSound(IFileInfo file)
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

	public ISoundEntry RegisterSound(string name, IFileInfo file)
		=> audioManagerProvider().RegisterSound(package.Manifest, name, file.ReadAllBytes());

	public ISoundEntry RegisterSound(byte[] data)
		=> audioManagerProvider().RegisterSound(package.Manifest, Guid.NewGuid().ToString(), data);

	public ISoundEntry RegisterSound(string name, byte[] data)
		=> audioManagerProvider().RegisterSound(package.Manifest, name, data);

	public ISoundInstance CreateInstance(ISoundEntry entry, bool started = true)
		=> entry.CreateInstance(this, started);
}

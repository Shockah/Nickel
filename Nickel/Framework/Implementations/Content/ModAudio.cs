using FMOD;
using Nanoray.PluginManager;
using System;
using System.IO;

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
		return audioManagerProvider().RegisterSound(package.Manifest, soundName, file.OpenRead);
	}

	public IModSoundEntry RegisterSound(string name, IFileInfo file)
		=> audioManagerProvider().RegisterSound(package.Manifest, name, file.OpenRead);

	public IModSoundEntry RegisterSound(Func<Stream> streamProvider)
		=> audioManagerProvider().RegisterSound(package.Manifest, Guid.NewGuid().ToString(), streamProvider);

	public IModSoundEntry RegisterSound(string name, Func<Stream> streamProvider)
		=> audioManagerProvider().RegisterSound(package.Manifest, name, streamProvider);
	
	public TEntry RegisterSound<TEntry, TArgs>(ICustomSoundEntryFactory<TEntry, TArgs> factory, string name, TArgs args) where TEntry : ICustomSoundEntry
		=> audioManagerProvider().RegisterSound(factory, package.Manifest, name, args);

	public TEntry RegisterSound<TEntry, TArgs>(string name, TArgs args)
		where TEntry : ICustomSoundEntry
		where TArgs : ICustomSoundEntryArgsWithDefaultFactory<TEntry, TArgs>
		=> audioManagerProvider().RegisterSound(TArgs.DefaultFactory, package.Manifest, name, args);

	public void RegisterBank(IFileInfo file)
		=> audioManagerProvider().RegisterBank(package.Manifest, file.OpenRead);

	public void RegisterBank(Func<Stream> streamProvider)
		=> audioManagerProvider().RegisterBank(package.Manifest, streamProvider);

	public ISoundInstance CreateInstance(ISoundEntry entry, bool started = true)
		=> entry.CreateInstance(started);
}

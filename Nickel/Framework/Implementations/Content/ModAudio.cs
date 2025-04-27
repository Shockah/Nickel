using FMOD;
using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nickel;

internal sealed class ModAudio(
	IPluginPackage<IModManifest> package,
	Func<AudioManager> audioManagerProvider,
	ILogger logger
) : IModAudio
{
	public IReadOnlyDictionary<string, IModSoundEntry> RegisteredSounds
		=> this.RegisteredSoundStorage;
	
	private readonly Dictionary<string, IModSoundEntry> RegisteredSoundStorage = [];
	
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
		
		if (!file.Exists)
			logger.LogWarning("Registering a sound `{Name}` from path `{Path}` that does not exist.", soundName, file.FullName);
		
		var entry = audioManagerProvider().RegisterSound(package.Manifest, soundName, file.OpenRead);
		this.RegisteredSoundStorage[soundName] = entry;
		return entry;
	}

	public IModSoundEntry RegisterSound(string name, IFileInfo file)
	{
		if (!file.Exists)
			logger.LogWarning("Registering a sound `{Name}` from path `{Path}` that does not exist.", name, file.FullName);
		
		var entry = audioManagerProvider().RegisterSound(package.Manifest, name, file.OpenRead);
		this.RegisteredSoundStorage[name] = entry;
		return entry;
	}

	public IModSoundEntry RegisterSound(Func<Stream> streamProvider)
	{
		var name = Guid.NewGuid().ToString();
		var entry = audioManagerProvider().RegisterSound(package.Manifest, name, streamProvider);
		this.RegisteredSoundStorage[name] = entry;
		return entry;
	}

	public IModSoundEntry RegisterSound(string name, Func<Stream> streamProvider)
	{
		var entry = audioManagerProvider().RegisterSound(package.Manifest, name, streamProvider);
		this.RegisteredSoundStorage[name] = entry;
		return entry;
	}
	
	public TEntry RegisterSound<TEntry, TArgs>(ICustomSoundEntryFactory<TEntry, TArgs> factory, TArgs args) where TEntry : ICustomSoundEntry
		=> audioManagerProvider().RegisterSound(factory, package.Manifest, factory.GetDefaultName(package.Manifest, args), args);
	
	public TEntry RegisterSound<TEntry, TArgs>(ICustomSoundEntryFactory<TEntry, TArgs> factory, string name, TArgs args) where TEntry : ICustomSoundEntry
		=> audioManagerProvider().RegisterSound(factory, package.Manifest, name, args);

	public TEntry RegisterSound<TEntry, TArgs>(TArgs args)
		where TEntry : ICustomSoundEntry
		where TArgs : ICustomSoundEntryArgsWithDefaultFactory<TEntry, TArgs>
		=> audioManagerProvider().RegisterSound(TArgs.DefaultFactory, package.Manifest, TArgs.DefaultFactory.GetDefaultName(package.Manifest, args), args);

	public TEntry RegisterSound<TEntry, TArgs>(string name, TArgs args)
		where TEntry : ICustomSoundEntry
		where TArgs : ICustomSoundEntryArgsWithDefaultFactory<TEntry, TArgs>
		=> audioManagerProvider().RegisterSound(TArgs.DefaultFactory, package.Manifest, name, args);

	public void RegisterBank(IFileInfo file)
	{
		if (!file.Exists)
			logger.LogWarning("Registering a sound bank from path `{Path}` that does not exist.", file.FullName);
		audioManagerProvider().RegisterBank(package.Manifest, file.OpenRead);
	}

	public void RegisterBank(Func<Stream> streamProvider)
		=> audioManagerProvider().RegisterBank(package.Manifest, streamProvider);

	public Song? ObtainSongForEventId(GUID eventId)
		=> audioManagerProvider().ObtainSongForEventId(package.Manifest, eventId);
	
	public Song? ObtainSongForSound(IEventSoundEntry entry)
		=> audioManagerProvider().ObtainSongForSound(package.Manifest, entry);
}

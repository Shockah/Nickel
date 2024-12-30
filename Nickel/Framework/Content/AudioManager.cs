using FMOD;
using FMOD.Studio;
using FSPRO;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Nickel;

internal sealed class AudioManager
{
	private readonly Func<IModManifest, ILogger> LoggerProvider;
	private readonly AfterDbInitManager<ModSoundEntry> ModSoundManager;
	private readonly AfterDbInitManager<BankEntry> BankManager;
	private readonly EnumCasePool EnumCasePool;
	private readonly IModManifest VanillaModManifest;
	private readonly HashSet<GUID> HandledBanks = [];
	private readonly Dictionary<GUID, EventSoundEntry> IdToEventSounds = [];
	private readonly Dictionary<string, EventSoundEntry> UniqueNameToEventSounds = [];
	private readonly Dictionary<string, ModSoundEntry> UniqueNameToModSounds = [];
	private readonly Dictionary<string, ICustomSoundEntry> UniqueNameToCustomSounds = [];
	private readonly Dictionary<GUID, Song?> IdToSongs = [];
	private readonly Dictionary<Song, GUID> SongToIds = [];
	private bool InjectedAnyModSounds;

	public AudioManager(Func<IModManifest, ILogger> loggerProvider, Func<ModLoadPhaseState> currentModLoadPhaseProvider, EnumCasePool enumCasePool, IModManifest vanillaModManifest)
	{
		this.LoggerProvider = loggerProvider;
		this.ModSoundManager = new(currentModLoadPhaseProvider, this.Inject);
		this.BankManager = new(currentModLoadPhaseProvider, this.Inject);
		this.EnumCasePool = enumCasePool;
		this.VanillaModManifest = vanillaModManifest;

		AudioPatches.OnPlaySong += this.OnPlaySong;
	}

	private static string StandardizeUniqueName(string uniqueName)
		=> uniqueName.ToLower(CultureInfo.InvariantCulture);

	public void RegisterBank(IModManifest owner, Func<Stream> streamProvider)
		=> this.BankManager.QueueOrInject(new(owner, streamProvider));

	public IModSoundEntry RegisterSound(IModManifest owner, string name, Func<Stream> streamProvider)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";

		if (this.UniqueNameToModSounds.ContainsKey(StandardizeUniqueName(uniqueName)))
			throw new ArgumentException($"A sound with the unique name `{uniqueName}` is already registered");

		var entry = new ModSoundEntry(owner, uniqueName, name, streamProvider);
		this.UniqueNameToModSounds[StandardizeUniqueName(uniqueName)] = entry;
		this.ModSoundManager.QueueOrInject(entry);
		return entry;
	}

	public TEntry RegisterSound<TEntry, TArgs>(ICustomSoundEntryFactory<TEntry, TArgs> factory, IModManifest owner, string name, TArgs args) where TEntry : ICustomSoundEntry
	{
		var uniqueName = $"{owner.UniqueName}::{name}";
		
		if (this.UniqueNameToModSounds.ContainsKey(StandardizeUniqueName(uniqueName)))
			throw new ArgumentException($"A sound with the unique name `{uniqueName}` is already registered");

		var entry = factory.CreateEntry(owner, uniqueName, name, args);
		this.UniqueNameToCustomSounds[StandardizeUniqueName(entry.UniqueName)] = entry;
		return entry;
	}

	private void HandleAllBanks()
	{
		if (Audio.inst is not { } audio)
			return;
		if (!audio.didLoadBanks)
			throw new NullReferenceException($"{nameof(Audio)}.{nameof(Audio.didLoadBanks)} is `false`");

		Audio.Catch(audio.fmodStudioSystem.getBankList(out var banks));
		foreach (var bank in banks)
			this.HandleBank(this.VanillaModManifest, bank);
	}

	private void HandleBank(IModManifest owner, FMOD.Studio.Bank bank)
	{
		Audio.Catch(bank.getID(out var bankId));
		if (!this.HandledBanks.Add(bankId))
			return;

		Audio.Catch(bank.getEventList(out var eventDescriptions));
		foreach (var eventDescription in eventDescriptions)
		{
			Audio.Catch(eventDescription.getID(out var eventId));
			var eventIdString = eventId.ToSystemGuid().ToString();
			var entry = new EventSoundEntry(owner, eventIdString, eventIdString, bankId, eventId);
			this.IdToEventSounds[eventId] = entry;
			this.UniqueNameToEventSounds[eventIdString] = entry;
		}
	}

	public EventSoundEntry? LookupSoundByEventId(GUID eventId)
	{
		this.HandleAllBanks();
		return this.IdToEventSounds.GetValueOrDefault(eventId);
	}

	public ISoundEntry? LookupSoundByUniqueName(string uniqueName)
	{
		if (this.UniqueNameToModSounds.TryGetValue(StandardizeUniqueName(uniqueName), out var modEntry))
			return modEntry;
		if (this.UniqueNameToCustomSounds.TryGetValue(StandardizeUniqueName(uniqueName), out var customEntry))
			return customEntry;
		
		this.HandleAllBanks();
		return this.UniqueNameToEventSounds.GetValueOrDefault(uniqueName);
	}

	public Song? ObtainSongForEventId(IModManifest requester, GUID eventId)
	{
		if (eventId == Event.Songs_Epoch)
			return Song.Epoch;
		if (this.IdToSongs.TryGetValue(eventId, out var existingSong))
			return existingSong;
		if (this.LookupSoundByEventId(eventId) is not { } entry)
			return null;
		
		if (Audio.inst is not { } audio)
			throw new NullReferenceException($"{nameof(Audio)}.{nameof(Audio.inst)} is `null`");
		if (!audio.didLoadBanks)
			throw new InvalidOperationException($"{nameof(Audio)}.{nameof(Audio.didLoadBanks)} is `false`");
		
		return this.UnvalidatedCreateSongForSound(requester, entry, audio);
	}

	public Song? ObtainSongForSound(IModManifest requester, IEventSoundEntry entry)
	{
		if (entry.EventId == Event.Songs_Epoch)
			return Song.Epoch;
		if (this.IdToSongs.TryGetValue(entry.EventId, out var existingSong))
			return existingSong;
		
		if (Audio.inst is not { } audio)
			throw new NullReferenceException($"{nameof(Audio)}.{nameof(Audio.inst)} is `null`");
		if (!audio.didLoadBanks)
			throw new InvalidOperationException($"{nameof(Audio)}.{nameof(Audio.didLoadBanks)} is `false`");

		return this.UnvalidatedCreateSongForSound(requester, entry, audio);
	}

	private Song? UnvalidatedCreateSongForSound(IModManifest requester, IEventSoundEntry entry, Audio audio)
	{
		Audio.Catch(audio.fmodStudioSystem.getEventByID(entry.EventId, out var @event));
		ValidateParameter("MenuLowPass", LogLevel.Warning);
		ValidateParameter("Combat", LogLevel.Debug);
		ValidateParameter("SceneLayer", LogLevel.Debug);
		ValidateParameter("NoTransition", LogLevel.Debug);

		var song = this.EnumCasePool.ObtainEnumCase<Song>();
		this.IdToSongs[entry.EventId] = song;
		this.SongToIds[song] = entry.EventId;
		return song;

		void ValidateParameter(string parameterName, LogLevel logLevel)
		{
			var result = @event.getParameterDescriptionByName(parameterName, out _);
			if (result == RESULT.OK)
				return;
			
			this.LoggerProvider(requester).Log(logLevel, "Requested FMOD bank song `{UniqueName}` does not have a `{ParameterName}` parameter or it is invalid.", entry.UniqueName, parameterName);
		}
	}

	internal void InjectQueuedEntries()
		=> this.ModSoundManager.InjectQueuedEntries();

	private void Inject(ModSoundEntry entry)
	{
		if (entry.SoundStorage is not null)
			return;
		if (Audio.inst is not { } audio)
			throw new NullReferenceException($"{nameof(Audio)}.{nameof(Audio.inst)} is `null`");
		if (entry.StreamProvider is null)
			throw new NullReferenceException($"{nameof(ModSoundEntry)}.{nameof(ModSoundEntry.StreamProvider)} is `null`");
		
		Audio.Catch(audio.fmodStudioSystem.getCoreSystem(out var coreSystem));

		var data = entry.StreamProvider().ToMemoryStream().ToArray();
		
		var soundInfo = new CREATESOUNDEXINFO
		{
			cbsize = MarshalHelper.SizeOf(typeof(CREATESOUNDEXINFO)),
			length = (uint)data.Length,
		};
		Audio.Catch(coreSystem.createSound(data, MODE.DEFAULT | MODE.OPENMEMORY | MODE.CREATESAMPLE, ref soundInfo, out var sound));

		if (!this.InjectedAnyModSounds)
		{
			this.InjectedAnyModSounds = true;
			Audio.Catch(audio.fmodStudioSystem.getBus("bus:/Sfx", out var bus));
			Audio.Catch(bus.lockChannelGroup());
		}

		entry.StreamProvider = null;
		entry.SoundStorage = sound;
	}

	private void Inject(BankEntry entry)
	{
		if (Audio.inst is not { } audio)
			throw new NullReferenceException($"{nameof(Audio)}.{nameof(Audio.inst)} is `null`");
		
		var data = entry.StreamProvider().ToMemoryStream().ToArray();

		Audio.Catch(audio.fmodStudioSystem.loadBankMemory(data, LOAD_BANK_FLAGS.NORMAL, out var bank));
		this.HandleBank(entry.Owner, bank);
	}
	
	private void OnPlaySong(object? _, AudioPatches.PlaySongArgs e)
	{
		if (!this.SongToIds.TryGetValue(e.MusicState.scene, out var actualEventId))
			return;
		
		e.Id = actualEventId;
		e.MusicState = e.MusicState with { scene = 0 };
	}

	private record BankEntry(
		IModManifest Owner,
		Func<Stream> StreamProvider
	);
}

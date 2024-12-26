using FMOD;
using FMOD.Studio;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Nickel;

internal sealed class AudioManager
{
	private readonly AfterDbInitManager<ModSoundEntry> ModSoundManager;
	private readonly AfterDbInitManager<BankEntry> BankManager;
	private readonly IModManifest VanillaModManifest;
	private readonly HashSet<GUID> HandledBanks = [];
	private readonly Dictionary<GUID, EventSoundEntry> IdToEventSounds = [];
	private readonly Dictionary<string, EventSoundEntry> UniqueNameToEventSounds = [];
	private readonly Dictionary<string, ModSoundEntry> UniqueNameToModSounds = [];
	private readonly Dictionary<string, ISoundEntry> UniqueNameToCustomSounds = [];

	public AudioManager(Func<ModLoadPhaseState> currentModLoadPhaseProvider, IModManifest vanillaModManifest)
	{
		this.ModSoundManager = new(currentModLoadPhaseProvider, Inject);
		this.BankManager = new(currentModLoadPhaseProvider, this.Inject);
		this.VanillaModManifest = vanillaModManifest;
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

	public void RegisterSoundEntry(ISoundEntry entry)
	{
		if (this.UniqueNameToModSounds.ContainsKey(StandardizeUniqueName(entry.UniqueName)))
			throw new ArgumentException($"A sound with the unique name `{entry.UniqueName}` is already registered");
		
		this.UniqueNameToCustomSounds[StandardizeUniqueName(entry.UniqueName)] = entry;
	}

	private void HandleAllBanks()
	{
		if (Audio.inst is not { } audio)
			return;

		Audio.Catch(audio.fmodStudioSystem.getBankList(out var banks));
		foreach (var bank in banks)
			this.HandleBank(this.VanillaModManifest, bank);
	}

	private void HandleBank(IModManifest owner, Bank bank)
	{
		Audio.Catch(bank.getID(out var bankId));
		if (!this.HandledBanks.Add(bankId))
			return;

		Audio.Catch(bank.getEventList(out var eventDescriptions));
		foreach (var eventDescription in eventDescriptions)
		{
			Audio.Catch(eventDescription.getID(out var eventId));
			var eventIdString = FmodGuidToString(eventId);
			var entry = new EventSoundEntry(owner, eventIdString, eventIdString, bankId, eventId);
			this.IdToEventSounds[eventId] = entry;
			this.UniqueNameToEventSounds[eventIdString] = entry;
		}
	}
	
	private static string FmodGuidToString(GUID guid)
		=> $"{(uint)guid.Data1:X4}:{(uint)guid.Data2:X4}:{(uint)guid.Data3:X4}:{(uint)guid.Data4:X4}";

	public IEventSoundEntry? LookupSoundByEventId(GUID eventId)
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

	internal void InjectQueuedEntries()
		=> this.ModSoundManager.InjectQueuedEntries();

	private static void Inject(ModSoundEntry entry)
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

	private record BankEntry(
		IModManifest Owner,
		Func<Stream> StreamProvider
	);
}

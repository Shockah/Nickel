using FMOD;
using FMOD.Studio;
using System;
using System.Collections.Generic;
using System.Globalization;

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

	public AudioManager(Func<ModLoadPhaseState> currentModLoadPhaseProvider, IModManifest vanillaModManifest)
	{
		this.ModSoundManager = new(currentModLoadPhaseProvider, Inject);
		this.BankManager = new(currentModLoadPhaseProvider, this.Inject);
		this.VanillaModManifest = vanillaModManifest;
	}

	private static string StandardizeUniqueName(string uniqueName)
		=> uniqueName.ToLower(CultureInfo.InvariantCulture);

	public void RegisterBank(byte[] data)
		=> this.BankManager.QueueOrInject(new(data));

	public IModSoundEntry RegisterSound(IModManifest owner, string name, byte[] data)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";

		if (this.UniqueNameToModSounds.ContainsKey(StandardizeUniqueName(uniqueName)))
			throw new ArgumentException($"A sprite with the unique name `{uniqueName}` is already registered.");

		var entry = new ModSoundEntry(owner, uniqueName, name, data);
		this.UniqueNameToModSounds[StandardizeUniqueName(uniqueName)] = entry;
		this.ModSoundManager.QueueOrInject(entry);
		return entry;
	}

	private void HandleAllBanks()
	{
		if (Audio.inst is not { } audio)
			return;

		Audio.Catch(audio.fmodStudioSystem.getBankList(out var banks));
		foreach (var bank in banks)
			this.HandleBank(bank);
	}

	private void HandleBank(Bank bank)
	{
		Audio.Catch(bank.getID(out var bankId));
		if (!this.HandledBanks.Add(bankId))
			return;

		Audio.Catch(bank.getEventList(out var eventDescriptions));
		foreach (var eventDescription in eventDescriptions)
		{
			Audio.Catch(eventDescription.getID(out var eventId));
			var eventIdString = FmodGuidToString(eventId);
			// TODO: pass in correct manifest after allowing mods to load their own banks
			var entry = new EventSoundEntry(this.VanillaModManifest, eventIdString, eventIdString, bankId, eventId);
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
		if (entry.Data is null)
			throw new NullReferenceException($"{nameof(ModSoundEntry)}.{nameof(ModSoundEntry.Data)} is `null`");
		
		Audio.Catch(audio.fmodStudioSystem.getCoreSystem(out var coreSystem));
		
		var soundInfo = new CREATESOUNDEXINFO
		{
			cbsize = MarshalHelper.SizeOf(typeof(CREATESOUNDEXINFO)),
			length = (uint)entry.Data.Length,
		};
		Audio.Catch(coreSystem.createSound(entry.Data, MODE.DEFAULT | MODE.OPENMEMORY | MODE.CREATESAMPLE, ref soundInfo, out var sound));

		entry.Data = null;
		entry.SoundStorage = sound;
	}

	private void Inject(BankEntry entry)
	{
		if (Audio.inst is not { } audio)
			throw new NullReferenceException($"{nameof(Audio)}.{nameof(Audio.inst)} is `null`");

		Audio.Catch(audio.fmodStudioSystem.loadBankMemory(entry.Data, LOAD_BANK_FLAGS.NORMAL, out var bank));
		this.HandleBank(bank);
	}

	private record BankEntry(
		byte[] Data
	);
}

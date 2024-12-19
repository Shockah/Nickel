using FMOD;
using FSPRO;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Nickel;

internal sealed class AudioManager(
	Func<ModLoadPhaseState> currentModLoadPhaseProvider,
	IModManifest vanillaModManifest
)
{
	private readonly AfterDbInitManager<ModSoundEntry> Manager = new(currentModLoadPhaseProvider, Inject);
	private Dictionary<GUID, BuiltInSoundEntry>? IdToBuiltInSounds;
	private Dictionary<string, BuiltInSoundEntry>? UniqueNameToBuiltInSounds;
	private readonly Dictionary<string, ModSoundEntry> UniqueNameToModSounds = [];

	private static string StandardizeUniqueName(string uniqueName)
		=> uniqueName.ToLower(CultureInfo.InvariantCulture);

	public ISoundEntry RegisterSound(IModManifest owner, string name, byte[] data)
	{
		var uniqueName = $"{owner.UniqueName}::{name}";

		if (this.UniqueNameToModSounds.ContainsKey(StandardizeUniqueName(uniqueName)))
			throw new ArgumentException($"A sprite with the unique name `{uniqueName}` is already registered.");

		var entry = new ModSoundEntry(owner, uniqueName, name, data);
		this.UniqueNameToModSounds[StandardizeUniqueName(uniqueName)] = entry;
		this.Manager.QueueOrInject(entry);
		return entry;
	}

	[MemberNotNull(nameof(this.IdToBuiltInSounds), nameof(this.UniqueNameToBuiltInSounds))]
	private void SetupBuiltInSounds()
	{
		if (this.IdToBuiltInSounds is not null && this.UniqueNameToBuiltInSounds is not null)
			return;
		
		this.IdToBuiltInSounds = [];
		this.UniqueNameToBuiltInSounds = [];
			
		foreach (var field in typeof(Event).GetFields(BindingFlags.Public | BindingFlags.Static))
		{
			if (field.FieldType != typeof(GUID))
				continue;
			var fieldId = (GUID)field.GetValue(null)!;
			var fieldName = field.Name;
			var entry = new BuiltInSoundEntry(vanillaModManifest, fieldName, fieldName, fieldId);
			this.IdToBuiltInSounds[fieldId] = entry;
			this.UniqueNameToBuiltInSounds[entry.UniqueName] = entry;
		}
	}

	public ISoundEntry? LookupSoundById(GUID id)
	{
		this.SetupBuiltInSounds();
		return this.IdToBuiltInSounds.GetValueOrDefault(id);
	}

	public ISoundEntry? LookupSoundByUniqueName(string uniqueName)
	{
		if (this.UniqueNameToModSounds.TryGetValue(StandardizeUniqueName(uniqueName), out var modEntry))
			return modEntry;
		
		this.SetupBuiltInSounds();
		return this.UniqueNameToBuiltInSounds.GetValueOrDefault(uniqueName);
	}

	internal void InjectQueuedEntries()
		=> this.Manager.InjectQueuedEntries();

	private static void Inject(ModSoundEntry entry)
	{
		if (entry.Sound is not null)
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
		entry.Sound = sound;
	}
}

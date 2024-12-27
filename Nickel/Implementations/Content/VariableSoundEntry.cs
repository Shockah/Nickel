using System;

namespace Nickel;

/// <summary>
/// A sound entry that can vary its volume and pitch each time it is played.
/// </summary>
public sealed class VariableSoundEntry : ICustomSoundEntry
{
	/// <inheritdoc/>
	public IModManifest ModOwner { get; }
	
	/// <inheritdoc/>
	public string UniqueName { get; }
	
	/// <inheritdoc/>
	public string LocalName { get; }
	
	/// <summary>
	/// The parameters of this variable sound entry.
	/// </summary>
	public VariableSoundEntryArgs Args { get; }

	internal VariableSoundEntry(IModManifest modOwner, string uniqueName, string localName, VariableSoundEntryArgs args)
	{
		this.ModOwner = modOwner;
		this.UniqueName = uniqueName;
		this.LocalName = localName;
		this.Args = args;
	}

	/// <inheritdoc/>
	public ISoundInstance CreateInstance(bool started = true)
	{
		var instance = this.Args.Wrapped.CreateInstance(started);
		
		var minVolume = Math.Min(this.Args.MinVolume, this.Args.MaxVolume);
		var maxVolume = Math.Max(this.Args.MinVolume, this.Args.MaxVolume);
		var volume = (float)(minVolume + Random.Shared.NextDouble() * (maxVolume - minVolume));
		instance.Volume = volume;
		
		var minPitch = Math.Min(this.Args.MinPitch, this.Args.MaxPitch);
		var maxPitch = Math.Max(this.Args.MinPitch, this.Args.MaxPitch);
		var pitch = (float)(minPitch + Random.Shared.NextDouble() * (maxPitch - minPitch));
		instance.Pitch = pitch;
		
		return new Instance(this, instance, volume, pitch);
	}

	private sealed class Instance(VariableSoundEntry entry, ISoundInstance instance, float volume, float pitch) : ISoundInstance
	{
		public ISoundEntry Entry { get; } = entry;

		public bool IsPaused
		{
			get => instance.IsPaused;
			set => instance.IsPaused = value;
		}

		public float Volume
		{
			get => volume <= 0 ? 0 : instance.Volume / volume;
			set => instance.Volume = value * volume;
		}
		
		public float Pitch
		{
			get => pitch <= 0 ? 0 : instance.Pitch / pitch;
			set => instance.Pitch = value * pitch;
		}
	}
}

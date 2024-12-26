using System;

namespace Nickel;

/// <summary>
/// A sound entry that can vary its volume and pitch each time it is played.
/// </summary>
/// <param name="modOwner">The mod that owns this content.</param>
/// <param name="localName">The local (mod-level) name of the sound. This has to be unique across the mod. This is usually a file path relative to the mod's package root.</param>
/// <param name="wrapped">The sound entry wrapped by this entry and used whenever a sound is to be played.</param>
/// <param name="configuration">The parameters of this variable sound entry.</param>
public sealed class VariableSoundEntry(
	IModManifest modOwner,
	string localName,
	ISoundEntry wrapped,
	VariableSoundEntryConfiguration configuration
) : ISoundEntry
{
	/// <inheritdoc/>
	public IModManifest ModOwner { get; } = modOwner;
	
	/// <inheritdoc/>
	public string UniqueName { get; } = $"{modOwner.UniqueName}::{localName}";
	
	/// <inheritdoc/>
	public string LocalName { get; } = localName;

	/// <summary>
	/// The sound wrapped by this object.
	/// </summary>
	public ISoundEntry Wrapped { get; } = wrapped;
	
	/// <summary>
	/// The parameters of this variable sound entry.
	/// </summary>
	public VariableSoundEntryConfiguration Configuration { get; } = configuration;

	/// <inheritdoc/>
	public ISoundInstance CreateInstance(bool started = true)
	{
		var instance = this.Wrapped.CreateInstance(started);
		var minVolume = Math.Min(this.Configuration.MinVolume, this.Configuration.MaxVolume);
		var maxVolume = Math.Max(this.Configuration.MinVolume, this.Configuration.MaxVolume);
		var minPitch = Math.Min(this.Configuration.MinPitch, this.Configuration.MaxPitch);
		var maxPitch = Math.Max(this.Configuration.MinPitch, this.Configuration.MaxPitch);
		var volume = minVolume + Random.Shared.NextDouble() * (maxVolume - minVolume);
		var pitch = minPitch + Random.Shared.NextDouble() * (maxPitch - minPitch);
		return new Instance(this, instance, (float)volume, (float)pitch);
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
			get => volume <= 0 ? 0 : instance.Pitch / pitch;
			set => instance.Pitch = value * pitch;
		}
	}
}

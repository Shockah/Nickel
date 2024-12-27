namespace Nickel;

/// <summary>
/// A factory that can create sound entries which can vary their volume and pitch each time they are played.
/// </summary>
public sealed class VariableSoundEntryFactory : ICustomSoundEntryFactory<VariableSoundEntry, VariableSoundEntryArgs>
{
	/// <summary>
	/// The default shared instance of the factory.
	/// </summary>
	public static VariableSoundEntryFactory Instance { get; } = new();
	
	/// <inheritdoc/>
	public string GetUniqueName(IModManifest owner, string localName, VariableSoundEntryArgs args)
		=> $"{owner.UniqueName}::{localName}[{args.Wrapped.UniqueName}]";

	/// <inheritdoc/>
	public VariableSoundEntry CreateEntry(IModManifest owner, string uniqueName, string localName, VariableSoundEntryArgs args)
		=> new(owner, uniqueName, localName, args);
}

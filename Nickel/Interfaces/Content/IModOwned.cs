namespace Nickel;

/// <summary>
/// Describes a mod-owned piece of content.
/// </summary>
public interface IModOwned
{
	/// <summary>The mod that owns this content.</summary>
	IModManifest ModOwner { get; }

	/// <summary>
	/// The unique name for the content.<br/>
	/// This name is of the <c>{ModUniqueName}::{ContentName}</c> format.
	/// </summary>
	string UniqueName { get; }
}

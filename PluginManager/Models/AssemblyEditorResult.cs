using System.Collections.Generic;

namespace Nanoray.PluginManager;

/// <summary>
/// The result of <see cref="IAssemblyEditor.EditAssemblyStream"/>.
/// </summary>
public readonly struct AssemblyEditorResult
{
	/// <summary>The level of a message the editor has reported.</summary>
	public enum MessageLevel
	{
		/// <summary>A message useful for debugging.</summary>
		Debug,
		
		/// <summary>An information message which might be useful for users.</summary>
		Info,
		
		/// <summary>A warning message which should be presented to users.</summary>
		Warning,
		
		/// <summary>An error message which should be presented to users.</summary>
		Error
	}

	/// <summary>
	/// Represents a message the editor has reported.
	/// </summary>
	/// <param name="Level">The level of a message the editor has reported.</param>
	/// <param name="Content">The actual text content of the message.</param>
	public record struct Message(
		MessageLevel Level,
		string Content
	);
	
	/// <summary>Any messages the editor has reported.</summary>
	public required IReadOnlyList<Message> Messages { get; init; }
}

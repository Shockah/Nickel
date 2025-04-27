using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Nanoray.PluginManager;

/// <summary>
/// The result of <see cref="IAssemblyEditor.EditAssemblyStream"/>.
/// </summary>
public readonly struct AssemblyEditorResult : IEquatable<AssemblyEditorResult>
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

	/// <inheritdoc/>
	public override bool Equals([NotNullWhen(true)] object? obj)
		=> obj is AssemblyEditorResult other && this.Messages.SequenceEqual(other.Messages);

	/// <inheritdoc/>
	public bool Equals(AssemblyEditorResult other)
		=> this.Messages.Equals(other.Messages);

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		var hashcode = new HashCode();
		foreach (var message in this.Messages)
			hashcode.Add(message);
		return hashcode.ToHashCode();
	}
}

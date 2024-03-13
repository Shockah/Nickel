using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// Describes an additional mod to load as part of another mod.
/// </summary>
public interface ISubmodEntry
{
	/// <summary>The manifest of the submod.</summary>
	IModManifest Manifest { get; }

	/// <summary>Whether the submod is optional. Optional submods do not produce errors if they cannot be loaded.</summary>
	bool IsOptional { get; }

	/// <summary>Additional submod data that couldn't be mapped.</summary>
	IReadOnlyDictionary<string, object> ExtensionData { get; }
}

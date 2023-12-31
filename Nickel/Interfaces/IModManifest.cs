using Nickel.Common;
using System.Collections.Generic;

namespace Nickel;

public interface IModManifest
{
	string UniqueName { get; }

	SemanticVersion Version { get; }

	SemanticVersion RequiredApiVersion { get; }

	IReadOnlySet<ModDependency> Dependencies { get; }

	string? DisplayName { get; }

	string? Description { get; }

	string? Author { get; }

	string ModType { get; }

	ModLoadPhase LoadPhase { get; }

	IReadOnlyList<ISubmodEntry> Submods { get; }

	IReadOnlyDictionary<string, object> ExtensionData { get; }
}

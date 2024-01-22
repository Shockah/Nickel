using Nickel.Common;
using System.Collections.Generic;
using System.Text;

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

public static class IModManifestExt
{
	public static string GetDisplayName(this IModManifest manifest, bool @long)
	{
		StringBuilder sb = new();
		sb.Append(string.IsNullOrEmpty(manifest.DisplayName) ? manifest.UniqueName : $"{manifest.DisplayName} ({manifest.UniqueName})");
		sb.Append($" {manifest.Version}");
		if (@long)
		{
			if (!string.IsNullOrEmpty(manifest.Author))
				sb.Append($" by {manifest.Author}");
			if (!string.IsNullOrEmpty(manifest.Description))
				sb.Append($": {manifest.Description}");
		}
		return sb.ToString();
	}
}

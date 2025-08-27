using Nickel.Common;
using System.Collections.Generic;
using System.Text;

namespace Nickel;

/// <summary>
/// Describes a mod's manifest file.
/// </summary>
public interface IModManifest
{
	/// <summary>
	/// The unique name of the mod.<br/>
	/// The mod loader uses this field to resolve dependencies, and will refuse to load multiple mods with the same <c>UniqueName</c>.
	/// </summary>
	string UniqueName { get; }

	/// <summary>The mod's version.</summary>
	SemanticVersion Version { get; }

	/// <summary>The mod's dependencies on other mods. The mod loader will ensure these are loaded first.</summary>
	IReadOnlySet<ModDependency> Dependencies { get; }

	/// <summary>The minimum version of the game supported by this mod.</summary>
	SemanticVersion? MinimumGameVersion { get; }

	/// <summary>The first version of the game that is unsupported by this mod.</summary>
	SemanticVersion? UnsupportedGameVersion { get; }

	/// <summary>An optional display ("nice") name of the mod presented to the user.</summary>
	string? DisplayName { get; }

	/// <summary>An optional description of the mod presented to the user.</summary>
	string? Description { get; }

	/// <summary>An optional field listing the author(s) of the mod.</summary>
	string? Author { get; }

	/// <summary>
	/// The type of the mod.<br/>
	/// See also: <seealso cref="NickelConstants.ModType"/>
	/// </summary>
	string ModType { get; }

	/// <summary>The phase to load the mod in.</summary>
	ModLoadPhase LoadPhase { get; }

	/// <summary>Additional mods to load as part of this mod.</summary>
	IReadOnlyList<ISubmodEntry> Submods { get; }

	/// <summary>Additional manifest data that couldn't be mapped.</summary>
	IReadOnlyDictionary<string, object> ExtensionData { get; }
}

/// <summary>
/// Hosts extension methods for mod manifests.
/// </summary>
public static class IModManifestExt
{
	/// <summary>
	/// Builds an as-nice-as-possible description for a mod, to be presented to the user.
	/// </summary>
	/// <param name="manifest">The mod's manifest.</param>
	/// <param name="long">Whether to build a long description, including the mod's author(s) and the <seealso cref="IModManifest.Description"/> field.</param>
	/// <returns>The description to be presented to the user.</returns>
	public static string GetDisplayName(this IModManifest manifest, bool @long)
	{
		var sb = new StringBuilder();
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

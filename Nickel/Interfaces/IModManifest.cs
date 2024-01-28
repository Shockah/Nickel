using Nickel.Common;
using System.Collections.Generic;
using System.Text;

namespace Nickel;

/**
 * <summary>The manifest data of a mod.</summary>
 */
public interface IModManifest
{
	/** <summary>The unique identifier for this mod.</summary> */
	string UniqueName { get; }

	/** <summary>The current version of this mod.</summary> */
	SemanticVersion Version { get; }

	/** <summary>The minimum required version of Nickel to run this mod.</summary> */
	SemanticVersion RequiredApiVersion { get; }

	/** <summary>The required dependencies for this mod.</summary> */
	IReadOnlySet<ModDependency> Dependencies { get; }

	/**
	 * <summary>The friendly display name for this mod.</summary>
	 * <remarks>This may be shown to the user to identify the mod.</remarks>
	 * <seealso cref="IModManifestExt.GetDisplayName"/>
	 */
	string? DisplayName { get; }

	/**
	 * <summary>The mod's description.</summary>
	 * <remarks>This should be a short description of what this mod does.</remarks>
	 */
	string? Description { get; }

	/** <summary>The mod author.</summary> */
	string? Author { get; }

	/**
	 * <summary>The mod type.</summary>
	 * <remarks>
	 * This is used to determine how to load this mod.
	 * The default mod type is <c>"Nickel"</c>; other mods may add additional types with their own loading routines.
	 * </remarks>
	 */
	string ModType { get; }

	/**
	 * <summary>When this mod is loaded.</summary>
	 * <remarks>Depending on the load phase, different functionality may be available.</remarks>
	 * <seealso cref="ModLoadPhase"/>
	 */
	ModLoadPhase LoadPhase { get; }

	/** <summary>Additional, subordinate mods included within this mod.</summary> */
	IReadOnlyList<ISubmodEntry> Submods { get; }

	/** <summary>Additional data about this mod that is not used by Nickel.</summary> */
	IReadOnlyDictionary<string, object> ExtensionData { get; }
}

/**
 * <summary>Contains helper extension methods for <see cref="IModManifest"/>s.</summary>
 */
public static class IModManifestExt
{
	/**
	 * <summary>Returns the display name for a given mod's manifest.</summary>
	 * <remarks>
	 * The display name includes the mod's name and <see cref="IModManifest.Version">version</see>.
	 * If <paramref name="long"/> is <c>true</c>, it also includes
	 * the <see cref="IModManifest.Author">author</see> and <see cref="IModManifest.Description">description</see>, if any.
	 * </remarks>
	 * <seealso cref="IModManifest.DisplayName"/>
	 * <seealso cref="IModManifest.UniqueName"/>
	 */
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

using Nickel.Common;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Nickel;

/**
 * <summary>Provides access to other mods.</summary>
 */
public interface IModRegistry
{
	/** <summary>The <see cref="IModManifest"/> for Cobalt Core itself.</summary>*/
	IModManifest VanillaModManifest { get; }
	/** <summary>A dictionary of all currently loaded mods, keyed by their <see cref="IModManifest.UniqueName"/>.</summary> */
	IReadOnlyDictionary<string, IModManifest> LoadedMods { get; }
	/** <summary>The <see cref="DirectoryInfo"/> to the mod library directory.</summary>*/
	DirectoryInfo ModsDirectory { get; }

	bool TryProxy<TProxy>(object @object, [MaybeNullWhen(false)] out TProxy proxy) where TProxy : class;

	TProxy? AsProxy<TProxy>(object? @object) where TProxy : class;

	/**
	 * <summary>Get the API implementation of a given mod.</summary>
	 * <param name="uniqueName">The <see cref="IModManifest.UniqueName"/> of the target mod.</param>
	 * <param name="minimumVersion">The minimum version of the target mod.</param>
	 * <typeparam name="TApi">A compatible interface type from your mod's assembly.</typeparam>
	 * <returns>A proxied API implementation, or <c>null</c> if proxying failed.</returns>
	 * <remarks>
	 * If <paramref name="minimumVersion"/> is non-<c>null</c> and the target mod is installed, but at a lower version, this method returns <c>null</c>.
	 * </remarks>
	 */
	TApi? GetApi<TApi>(string uniqueName, SemanticVersion? minimumVersion = null) where TApi : class;
}

using Nickel.Common;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Nickel;

/// <summary>
/// A mod-specific mod registry.<br/>
/// Allows retrieving global mod loader state, checking for all loaded mods and communicating with other mods' APIs.
/// </summary>
public interface IModRegistry
{
	/// <summary>A virtual manifest, representing the vanilla game itself.</summary>
	IModManifest VanillaModManifest { get; }
	
	/// <summary>A virtual manifest, representing the mod loader itself.</summary>
	IModManifest ModLoaderModManifest { get; }

	/// <summary>A dictionary mapping all loaded mods' <see cref="IModManifest.UniqueName"/>s to their manifests.</summary>
	IReadOnlyDictionary<string, IModManifest> LoadedMods { get; }

	/// <summary>The directory containing the mods.</summary>
	DirectoryInfo ModsDirectory { get; }

	/// <summary>
	/// Tries to proxy a given object to a given interface.
	/// </summary>
	/// <typeparam name="TProxy">The interface type to proxy the object to.</typeparam>
	/// <param name="object">The object to proxy.</param>
	/// <param name="proxy">The proxied object, if proxying succeeds.</param>
	/// <returns>Whether proxying succeeded.</returns>
	bool TryProxy<TProxy>(object @object, [MaybeNullWhen(false)] out TProxy proxy) where TProxy : class;

	/// <summary>
	/// Proxies a given object to a given interface.
	/// </summary>
	/// <typeparam name="TProxy">The interface type to proxy the object to.</typeparam>
	/// <param name="object">The object to proxy.</param>
	/// <returns>The proxied object, or <c>null</c> if <see cref="@object"/> was <c>null</c> or it failed to proxy.</returns>
	TProxy? AsProxy<TProxy>(object? @object) where TProxy : class;

	/// <summary>
	/// Obtains another mod's API.<br/>
	/// The mod's API is proxied to the requested interface type.
	/// </summary>
	/// <typeparam name="TApi">The interface type to proxy the API to.</typeparam>
	/// <param name="uniqueName">The <see cref="IModManifest.UniqueName"/> of the mod providing the API.</param>
	/// <param name="minimumVersion">An optional minimum version of the mod providing the API.</param>
	/// <returns>The proxied API, or <c>null</c> if failed.</returns>
	TApi? GetApi<TApi>(string uniqueName, SemanticVersion? minimumVersion = null) where TApi : class;
}

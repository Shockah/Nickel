using System;
using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// A mod-specific artifact registry.
/// Allows looking up and registering artifacts.
/// </summary>
public interface IModArtifacts
{
	/// <summary>
	/// A dictionary containing all entries registered by the owner of this helper.
	/// </summary>
	IReadOnlyDictionary<string, IArtifactEntry> RegisteredArtifacts { get; }
	
	/// <summary>
	/// Lookup an <see cref="Artifact"/> entry by its class type.
	/// </summary>
	/// <param name="artifactType">The type to retrieve an entry for.</param>
	/// <returns>An entry, or <c>null</c> if the type does not match any known artifacts.</returns>
	IArtifactEntry? LookupByArtifactType(Type artifactType);
	
	/// <summary>
	/// Lookup an <see cref="Artifact"/> entry by its full <see cref="IModOwned.UniqueName"/>.
	/// </summary>
	/// <param name="uniqueName">The unique name to retrieve an entry for.</param>
	/// <returns>An entry, or <c>null</c> if the unique name does not match any known artifacts.</returns>
	IArtifactEntry? LookupByUniqueName(string uniqueName);
	
	/// <summary>
	/// Register a new <see cref="Artifact"/>.
	/// </summary>
	/// <param name="configuration">A configuration describing all aspects of the <see cref="Artifact"/>.</param>
	/// <returns>An entry for the new <see cref="Artifact"/>.</returns>
	IArtifactEntry RegisterArtifact(ArtifactConfiguration configuration);

	/// <summary>
	/// Register a new <see cref="Artifact"/>.
	/// </summary>
	/// <param name="name">The local (mod-level) name for the <see cref="Artifact"/>. This has to be unique across all artifacts in the mod.</param>
	/// <param name="configuration">A configuration describing all aspects of the <see cref="Artifact"/>.</param>
	/// <returns>An entry for the new <see cref="Artifact"/>.</returns>
	IArtifactEntry RegisterArtifact(string name, ArtifactConfiguration configuration);
}

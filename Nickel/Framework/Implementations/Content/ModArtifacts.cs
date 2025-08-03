using System;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class ModArtifacts(
	IModManifest modManifest,
	Func<ArtifactManager> artifactManagerProvider
) : IModArtifacts
{
	public IReadOnlyDictionary<string, IArtifactEntry> RegisteredArtifacts
		=> this.RegisteredArtifactStorage;
	
	private readonly Dictionary<string, IArtifactEntry> RegisteredArtifactStorage = [];

	public IArtifactEntry? LookupByArtifactType(Type artifactType)
		=> artifactManagerProvider().LookupByArtifactType(artifactType);

	public IArtifactEntry? LookupByUniqueName(string uniqueName)
		=> artifactManagerProvider().LookupByUniqueName(uniqueName);

	public IArtifactEntry RegisterArtifact(ArtifactConfiguration configuration)
	{
		var entry = artifactManagerProvider().RegisterArtifact(modManifest, configuration.ArtifactType.Name, configuration);
		this.RegisteredArtifactStorage[configuration.ArtifactType.Name] = entry;
		return entry;
	}

	public IArtifactEntry RegisterArtifact(string name, ArtifactConfiguration configuration)
	{
		var entry = artifactManagerProvider().RegisterArtifact(modManifest, name, configuration);
		this.RegisteredArtifactStorage[name] = entry;
		return entry;
	}
}

internal sealed class VanillaModArtifacts(
	Func<ArtifactManager> artifactManagerProvider
) : IModArtifacts
{
	private readonly Lazy<Dictionary<string, IArtifactEntry>> LazyRegisteredArtifacts = new(() => DB.artifacts.Where(kvp => kvp.Value.Assembly == typeof(Card).Assembly).ToDictionary(kvp => kvp.Key, kvp => artifactManagerProvider().LookupByArtifactType(kvp.Value)!));
	
	public IReadOnlyDictionary<string, IArtifactEntry> RegisteredArtifacts
		=>  this.LazyRegisteredArtifacts.Value;
	
	public IArtifactEntry? LookupByArtifactType(Type artifactType)
		=> artifactManagerProvider().LookupByArtifactType(artifactType);

	public IArtifactEntry? LookupByUniqueName(string uniqueName)
		=> artifactManagerProvider().LookupByUniqueName(uniqueName);

	public IArtifactEntry RegisterArtifact(ArtifactConfiguration configuration)
		=> throw new NotSupportedException();

	public IArtifactEntry RegisterArtifact(string name, ArtifactConfiguration configuration)
		=> throw new NotSupportedException();
}

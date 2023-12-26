using System;

namespace Nickel;

internal sealed class ModArtifacts : IModArtifacts
{
    private IModManifest ModManifest { get; init; }
    private Func<ArtifactManager> ArtifactManagerProvider { get; init; }

    public ModArtifacts(IModManifest modManifest, Func<ArtifactManager> artifactManagerProvider)
    {
        this.ModManifest = modManifest;
        this.ArtifactManagerProvider = artifactManagerProvider;
    }

    public IArtifactEntry RegisterArtifact(string name, ArtifactConfiguration configuration)
        => this.ArtifactManagerProvider().RegisterArtifact(this.ModManifest, name, configuration);
}

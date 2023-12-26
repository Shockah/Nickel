using System;

namespace Nickel;

public readonly struct ArtifactConfiguration
{
    public Type ArtifactType { get; init; }
    public ArtifactMeta Meta { get; init; }
    public Spr? Sprite { get; init; }
}

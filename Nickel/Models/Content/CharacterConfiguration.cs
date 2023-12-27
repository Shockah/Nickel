using System;
using System.Collections.Generic;

namespace Nickel;

public readonly struct CharacterConfiguration
{
    public Deck Deck { get; init; }
    public Spr BorderSprite { get; init; }
    public IReadOnlyList<Type> StarterArtifactTypes { get; init; }
    public IReadOnlyList<Type> StarterCardTypes { get; init; }
    public CharacterAnimationConfiguration? NeutralAnimation { get; init; }
    public CharacterAnimationConfiguration? MiniAnimation { get; init; }
    public bool IsLocked { get; init; }
}

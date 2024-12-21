namespace Nickel;

/// <summary>
/// Describes an animation for a <see cref="Character"/>.
/// </summary>
public interface ICharacterAnimationEntry : IModOwned
{
	/// <summary>The configuration used to register the <see cref="Character"/> animation.</summary>
	CharacterAnimationConfiguration Configuration { get; }
}

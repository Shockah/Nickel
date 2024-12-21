namespace Nickel;

/// <summary>
/// Describes an animation for a <see cref="Character"/>.
/// </summary>
public interface ICharacterAnimationEntryV2 : IModOwned
{
	/// <summary>The configuration used to register the <see cref="Character"/> animation.</summary>
	CharacterAnimationConfigurationV2 Configuration { get; }
}

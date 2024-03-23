namespace Nickel;

/// <summary>
/// A mod-specific character registry.
/// Allows looking up and registering characters.
/// </summary>
public interface IModCharacters
{
	ICharacterAnimationEntry RegisterCharacterAnimation(CharacterAnimationConfiguration configuration);
	ICharacterAnimationEntry RegisterCharacterAnimation(string name, CharacterAnimationConfiguration configuration);
	ICharacterEntry RegisterCharacter(string name, CharacterConfiguration configuration);
}

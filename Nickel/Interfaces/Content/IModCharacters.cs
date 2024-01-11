namespace Nickel;

public interface IModCharacters
{
	ICharacterAnimationEntry RegisterCharacterAnimation(CharacterAnimationConfiguration configuration);
	ICharacterAnimationEntry RegisterCharacterAnimation(string name, CharacterAnimationConfiguration configuration);
	ICharacterEntry RegisterCharacter(string name, CharacterConfiguration configuration);
}

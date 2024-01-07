namespace Nickel;

public interface IModCharacters
{
	ICharacterAnimationEntry RegisterCharacterAnimation(string name, CharacterAnimationConfiguration configuration);
	ICharacterEntry RegisterCharacter(string name, CharacterConfiguration configuration);
}

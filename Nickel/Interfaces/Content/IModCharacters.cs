namespace Nickel;

public interface IModCharacters
{
	ICharacterAnimationEntry RegisterCharacterAnimation(CharacterAnimationConfiguration configuration);
	ICharacterAnimationEntry RegisterCharacterAnimation(string name, CharacterAnimationConfiguration configuration);

	ICharacterEntry? LookupByDeck(Deck deck);
	ICharacterEntry? LookupByUniqueName(string uniqueName);
	ICharacterEntry RegisterCharacter(string name, CharacterConfiguration configuration);
}

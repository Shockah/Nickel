using System;

namespace Nickel;

internal sealed class ModCharacters(
	IModManifest modManifest,
	Func<CharacterManager> characterManagerProvider
) : IModCharacters
{
	public IModCharactersV2 V2 { get; } = new ModCharactersV2(modManifest, characterManagerProvider);

	public ICharacterAnimationEntry RegisterCharacterAnimation(CharacterAnimationConfiguration configuration)
		=> this.RegisterCharacterAnimation($"{configuration.Deck.Key()}::{configuration.LoopTag}", configuration);

	public ICharacterAnimationEntry RegisterCharacterAnimation(string name, CharacterAnimationConfiguration configuration)
		=> characterManagerProvider().RegisterCharacterAnimation(modManifest, name, configuration);

	public ICharacterEntry? LookupByDeck(Deck deck)
		=> characterManagerProvider().LookupByDeck(deck);

	public ICharacterEntry? LookupByUniqueName(string uniqueName)
		=> characterManagerProvider().LookupByUniqueName(uniqueName);

	public ICharacterEntry RegisterCharacter(string name, CharacterConfiguration configuration)
		=> characterManagerProvider().RegisterCharacter(modManifest, name, configuration);
}

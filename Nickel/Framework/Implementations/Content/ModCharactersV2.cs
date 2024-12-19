using System;

namespace Nickel;

internal sealed class ModCharactersV2(
	IModManifest modManifest,
	Func<CharacterManager> characterManagerProvider
) : IModCharactersV2
{
	public ICharacterAnimationEntryV2 RegisterCharacterAnimation(CharacterAnimationConfigurationV2 configuration)
		=> this.RegisterCharacterAnimation($"{configuration.CharacterType}::{configuration.LoopTag}", configuration);

	public ICharacterAnimationEntryV2 RegisterCharacterAnimation(string name, CharacterAnimationConfigurationV2 configuration)
		=> characterManagerProvider().RegisterCharacterAnimationV2(modManifest, name, configuration);

	public IPlayableCharacterEntryV2? LookupByDeck(Deck deck)
		=> characterManagerProvider().LookupByDeckV2(deck);

	public ICharacterEntryV2? LookupByCharacterType(string characterType)
		=> characterManagerProvider().LookupByCharacterTypeV2(characterType);

	public ICharacterEntryV2? LookupByUniqueName(string uniqueName)
		=> characterManagerProvider().LookupByUniqueNameV2(uniqueName);

	public IPlayableCharacterEntryV2 RegisterPlayableCharacter(string name, PlayableCharacterConfigurationV2 configuration)
		=> characterManagerProvider().RegisterPlayableCharacterV2(modManifest, name, configuration);

	public INonPlayableCharacterEntryV2 RegisterNonPlayableCharacter(string name, NonPlayableCharacterConfigurationV2 configuration)
		=> characterManagerProvider().RegisterNonPlayableCharacterV2(modManifest, name, configuration);
}

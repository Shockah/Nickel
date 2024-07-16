using System;

namespace Nickel;

internal sealed class ModCharactersV2 : IModCharactersV2
{
	private readonly IModManifest ModManifest;
	private readonly Func<CharacterManager> CharacterManagerProvider;

	public ModCharactersV2(IModManifest modManifest, Func<CharacterManager> characterManagerProvider)
	{
		this.ModManifest = modManifest;
		this.CharacterManagerProvider = characterManagerProvider;
	}

	public ICharacterAnimationEntryV2 RegisterCharacterAnimation(CharacterAnimationConfigurationV2 configuration)
		=> this.RegisterCharacterAnimation($"{configuration.CharacterType}::{configuration.LoopTag}", configuration);

	public ICharacterAnimationEntryV2 RegisterCharacterAnimation(string name, CharacterAnimationConfigurationV2 configuration)
		=> this.CharacterManagerProvider().RegisterCharacterAnimationV2(this.ModManifest, name, configuration);

	public IPlayableCharacterEntryV2? LookupByDeck(Deck deck)
		=> this.CharacterManagerProvider().LookupByDeckV2(deck);

	public ICharacterEntryV2? LookupByCharacterType(string characterType)
		=> this.CharacterManagerProvider().LookupByCharacterTypeV2(characterType);

	public ICharacterEntryV2? LookupByUniqueName(string uniqueName)
		=> this.CharacterManagerProvider().LookupByUniqueNameV2(uniqueName);

	public IPlayableCharacterEntryV2 RegisterPlayableCharacter(string name, PlayableCharacterConfigurationV2 configuration)
		=> this.CharacterManagerProvider().RegisterPlayableCharacterV2(this.ModManifest, name, configuration);

	public INonPlayableCharacterEntryV2 RegisterNonPlayableCharacter(string name, NonPlayableCharacterConfigurationV2 configuration)
		=> this.CharacterManagerProvider().RegisterNonPlayableCharacterV2(this.ModManifest, name, configuration);
}

using System;

namespace Nickel;

internal sealed class ModCharacters : IModCharacters
{
	private IModManifest ModManifest { get; }
	private Func<CharacterManager> CharacterManagerProvider { get; }

	public ModCharacters(IModManifest modManifest, Func<CharacterManager> characterManagerProvider)
	{
		this.ModManifest = modManifest;
		this.CharacterManagerProvider = characterManagerProvider;
	}

	public ICharacterAnimationEntry RegisterCharacterAnimation(CharacterAnimationConfiguration configuration)
		=> this.RegisterCharacterAnimation(configuration.LoopTag, configuration);

	public ICharacterAnimationEntry RegisterCharacterAnimation(string name, CharacterAnimationConfiguration configuration)
		=> this.CharacterManagerProvider().RegisterCharacterAnimation(this.ModManifest, name, configuration);

	public ICharacterEntry? LookupByDeck(Deck deck)
		=> this.CharacterManagerProvider().LookupByDeck(deck);

	public ICharacterEntry? LookupByUniqueName(string uniqueName)
		=> this.CharacterManagerProvider().LookupByUniqueName(uniqueName);

	public ICharacterEntry RegisterCharacter(string name, CharacterConfiguration configuration)
		=> this.CharacterManagerProvider().RegisterCharacter(this.ModManifest, name, configuration);
}

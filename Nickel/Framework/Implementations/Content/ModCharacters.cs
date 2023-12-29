using System;

namespace Nickel;

internal sealed class ModCharacters : IModCharacters
{
	private IModManifest ModManifest { get; init; }
	private Func<CharacterManager> CharacterManagerProvider { get; init; }

	public ModCharacters(IModManifest modManifest, Func<CharacterManager> characterManagerProvider)
	{
		this.ModManifest = modManifest;
		this.CharacterManagerProvider = characterManagerProvider;
	}

	public ICharacterAnimationEntry RegisterCharacterAnimation(string name, CharacterAnimationConfiguration configuration)
		=> this.CharacterManagerProvider().RegisterCharacterAnimation(this.ModManifest, name, configuration);
}

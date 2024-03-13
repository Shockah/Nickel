namespace Nickel;

/// <summary>
/// Describes a playable <see cref="Character"/>.
/// </summary>
public interface ICharacterEntry : IModOwned
{
	/// <summary>The configuration used to register the <see cref="Character"/>.</summary>
	CharacterConfiguration Configuration { get; }
	
	/// <summary>An entry for the <c>Character Is Missing</c> status specifically for this character.</summary>
	IStatusEntry MissingStatus { get; }
}

namespace Nickel;

/// <summary>
/// Describes a playable <see cref="Character"/>.
/// </summary>
public interface IPlayableCharacterEntry : ICharacterEntry
{
	/// <summary>The configuration used to register the playable <see cref="Character"/>.</summary>
	PlayableCharacterConfiguration Configuration { get; }
	
	/// <summary>An entry for the <c>Character Is Missing</c> status specifically for this character.</summary>
	IStatusEntry MissingStatus { get; }

	/// <summary>
	/// Amends a playable <see cref="Character"/>'s <see cref="PlayableCharacterConfiguration">configuration</see>.
	/// </summary>
	/// <param name="amends">The amends to make.</param>
	/// <remarks>
	/// This method is only valid for modded entries.
	/// </remarks>
	void Amend(PlayableCharacterConfiguration.Amends amends);
}

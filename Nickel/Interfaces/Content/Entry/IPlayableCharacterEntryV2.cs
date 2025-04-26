namespace Nickel;

/// <summary>
/// Describes a playable <see cref="Character"/>.
/// </summary>
public interface IPlayableCharacterEntryV2 : ICharacterEntryV2
{
	/// <summary>The configuration used to register the playable <see cref="Character"/>.</summary>
	PlayableCharacterConfigurationV2 Configuration { get; }
	
	/// <summary>An entry for the <c>Character Is Missing</c> status specifically for this character.</summary>
	IStatusEntry MissingStatus { get; }

	/// <summary>
	/// Amends a playable <see cref="Character"/>'s <see cref="PlayableCharacterConfigurationV2">configuration</see>.
	/// </summary>
	/// <param name="amends">The amends to make.</param>
	/// <remarks>
	/// This method is only valid for modded entries.
	/// </remarks>
	void Amend(PlayableCharacterConfigurationV2.Amends amends);
}

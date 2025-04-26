namespace Nickel;

/// <summary>
/// Describes a <see cref="Deck"/>.
/// </summary>
public interface IDeckEntry : IModOwned
{
	/// <summary>The <see cref="Deck"/> described by this entry.</summary>
	Deck Deck { get; }
	
	/// <summary>The configuration used to register the <see cref="Deck"/>.</summary>
	DeckConfiguration Configuration { get; }

	/// <summary>
	/// Amends a <see cref="Deck"/>'s <see cref="DeckConfiguration">configuration</see>.
	/// </summary>
	/// <param name="amends">The amends to make.</param>
	/// <remarks>
	/// This method is only valid for modded entries.
	/// </remarks>
	void Amend(DeckConfiguration.Amends amends);
}

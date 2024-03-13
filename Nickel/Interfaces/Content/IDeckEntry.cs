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
}

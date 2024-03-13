namespace Nickel;

/// <summary>
/// Describes a card trait (which usually appears in the bottom-right of a card).
/// </summary>
public interface ICardTraitEntry : IModOwned
{
	/// <summary>The configuration used to register the card trait.</summary>
	CardTraitConfiguration Configuration { get; }
}

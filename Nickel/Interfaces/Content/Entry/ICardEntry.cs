namespace Nickel;

/// <summary>
/// Describes a <see cref="Card"/>.
/// </summary>
public interface ICardEntry : IModOwned
{
	/// <summary>The configuration used to register the <see cref="Card"/>.</summary>
	CardConfiguration Configuration { get; }
}

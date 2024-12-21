namespace Nickel;

/// <summary>
/// Describes a <see cref="StarterShip"/>.
/// </summary>
public interface IShipEntry : IModOwned
{
	/// <summary>The configuration used to register the <see cref="StarterShip"/>.</summary>
	ShipConfiguration Configuration { get; }
}

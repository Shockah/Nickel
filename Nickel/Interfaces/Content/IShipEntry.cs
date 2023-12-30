namespace Nickel;

public interface IShipEntry : IModOwned
{
	ShipConfiguration Configuration { get; }
}

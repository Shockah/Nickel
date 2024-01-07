namespace Nickel;

public interface IModShips
{
	IShipEntry RegisterShip(string name, ShipConfiguration configuration);
	IPartEntry RegisterPart(string name, PartConfiguration configuration);
}

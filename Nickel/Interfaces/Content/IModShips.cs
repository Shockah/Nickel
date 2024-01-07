namespace Nickel;

public interface IModShips
{
	IShipEntry RegisterShip(string name, ShipConfiguration configuration);
	IPartTypeEntry RegisterPartType(string name, PartTypeConfiguration configuration);
	IPartEntry RegisterPart(string name, PartConfiguration configuration);
}

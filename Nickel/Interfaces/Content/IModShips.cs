namespace Nickel;

public partial interface IModShips
{
	IShipEntry RegisterShip(string name, ShipConfiguration starterShip);
	IPartEntry RegisterPart(string name, Spr onPart, Spr? offPart);
}

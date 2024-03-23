namespace Nickel;

/// <summary>
/// A mod-specific ship registry.
/// Allows looking up and registering ships and ship parts.
/// </summary>
public interface IModShips
{
	IShipEntry? LookupByUniqueName(string uniqueName);
	IShipEntry RegisterShip(string name, ShipConfiguration configuration);
	IPartTypeEntry RegisterPartType(string name, PartTypeConfiguration configuration);
	IPartEntry RegisterPart(string name, PartConfiguration configuration);
}

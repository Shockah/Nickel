using System;

namespace Nickel;

internal sealed class ModShips(
	IModManifest modManifest,
	Func<ShipManager> shipManagerProvider,
	Func<PartManager> partManagerProvider
) : IModShips
{
	public IShipEntry? LookupByUniqueName(string uniqueName)
		=> shipManagerProvider().LookupByUniqueName(uniqueName);

	public IShipEntry RegisterShip(string name, ShipConfiguration configuration)
		=> shipManagerProvider().RegisterShip(modManifest, name, configuration);

	public IPartTypeEntry RegisterPartType(string name, PartTypeConfiguration configuration)
		=> partManagerProvider().RegisterPartType(modManifest, name, configuration);

	public IPartEntry RegisterPart(string name, PartConfiguration configuration)
		=> partManagerProvider().RegisterPart(modManifest, name, configuration);
}

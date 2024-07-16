using System;

namespace Nickel;

internal sealed class ModShips : IModShips
{
	private readonly IModManifest ModManifest;
	private readonly Func<ShipManager> ShipManagerProvider;
	private readonly Func<PartManager> PartManagerProvider;

	public ModShips(
		IModManifest modManifest,
		Func<ShipManager> shipManagerProvider,
		Func<PartManager> partManagerProvider
	)
	{
		this.ModManifest = modManifest;
		this.ShipManagerProvider = shipManagerProvider;
		this.PartManagerProvider = partManagerProvider;
	}

	public IShipEntry? LookupByUniqueName(string uniqueName)
		=> this.ShipManagerProvider().LookupByUniqueName(uniqueName);

	public IShipEntry RegisterShip(string name, ShipConfiguration configuration)
		=> this.ShipManagerProvider().RegisterShip(this.ModManifest, name, configuration);

	public IPartTypeEntry RegisterPartType(string name, PartTypeConfiguration configuration)
		=> this.PartManagerProvider().RegisterPartType(this.ModManifest, name, configuration);

	public IPartEntry RegisterPart(string name, PartConfiguration configuration)
		=> this.PartManagerProvider().RegisterPart(this.ModManifest, name, configuration);
}

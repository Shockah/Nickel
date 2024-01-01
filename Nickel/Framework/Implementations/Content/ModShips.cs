using System;

namespace Nickel;

internal sealed class ModShips : IModShips
{
	private IModManifest ModManifest { get; }
	private Func<ShipManager> ShipManagerProvider { get; }
	private Func<PartManager> PartManagerProvider { get; }

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

	public IShipEntry RegisterShip(string name, ShipConfiguration configuration)
		=> this.ShipManagerProvider().RegisterShip(this.ModManifest, name, configuration);

	public IPartEntry RegisterPart(string name, PartConfiguration configuration)
		=> this.PartManagerProvider().RegisterPart(this.ModManifest, name, configuration);
}

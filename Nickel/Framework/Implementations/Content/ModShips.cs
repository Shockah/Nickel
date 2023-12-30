using System;

namespace Nickel;

internal sealed class ModShips : IModShips
{
	private IModManifest ModManifest { get; }
	private Func<ShipManager> ShipManagerProvider { get; }

	public ModShips(IModManifest modManifest, Func<ShipManager> ShipManagerProvider)
	{
		this.ModManifest = modManifest;
		this.ShipManagerProvider = ShipManagerProvider;
	}

	public IShipEntry RegisterShip(string name, ShipConfiguration configuration)
		=> this.ShipManagerProvider().RegisterShip(this.ModManifest, name, configuration);
}

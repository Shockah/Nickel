using System;

namespace Nickel;

internal sealed class ModStarterShips : IModStarterShips
{
	private IModManifest ModManifest { get; }
	private Func<StarterShipManager> StarterShipManagerProvider { get; }

	public ModStarterShips(IModManifest modManifest, Func<StarterShipManager> StarterShipManagerProvider)
	{
		this.ModManifest = modManifest;
		this.StarterShipManagerProvider = StarterShipManagerProvider;
	}

	public IStarterShipEntry RegisterStarterShip(string name, StarterShipConfiguration configuration)
		=> this.StarterShipManagerProvider().RegisterStarterShip(this.ModManifest, name, configuration);
}

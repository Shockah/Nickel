using System;

namespace Nickel;

internal sealed class ModStatuses : IModStatuses
{
	private IModManifest ModManifest { get; init; }
	private Func<StatusManager> StatusManagerProvider { get; init; }

	public ModStatuses(IModManifest modManifest, Func<StatusManager> statusManagerProvider)
	{
		this.ModManifest = modManifest;
		this.StatusManagerProvider = statusManagerProvider;
	}

	public IStatusEntry RegisterStatus(string name, StatusConfiguration configuration)
		=> this.StatusManagerProvider().RegisterStatus(this.ModManifest, name, configuration);
}

using System;

namespace Nickel;

internal sealed class ModStatuses : IModStatuses
{
	private readonly IModManifest ModManifest;
	private readonly Func<StatusManager> StatusManagerProvider;

	public ModStatuses(IModManifest modManifest, Func<StatusManager> statusManagerProvider)
	{
		this.ModManifest = modManifest;
		this.StatusManagerProvider = statusManagerProvider;
	}

	public IStatusEntry? LookupByStatus(Status status)
		=> this.StatusManagerProvider().LookupByStatus(status);

	public IStatusEntry? LookupByUniqueName(string uniqueName)
		=> this.StatusManagerProvider().LookupByUniqueName(uniqueName);

	public IStatusEntry RegisterStatus(string name, StatusConfiguration configuration)
		=> this.StatusManagerProvider().RegisterStatus(this.ModManifest, name, configuration);
}

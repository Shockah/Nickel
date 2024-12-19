using System;

namespace Nickel;

internal sealed class ModStatuses(
	IModManifest modManifest,
	Func<StatusManager> statusManagerProvider
) : IModStatuses
{
	public IStatusEntry? LookupByStatus(Status status)
		=> statusManagerProvider().LookupByStatus(status);

	public IStatusEntry? LookupByUniqueName(string uniqueName)
		=> statusManagerProvider().LookupByUniqueName(uniqueName);

	public IStatusEntry RegisterStatus(string name, StatusConfiguration configuration)
		=> statusManagerProvider().RegisterStatus(modManifest, name, configuration);
}

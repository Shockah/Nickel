using System;
using System.Collections.Generic;

namespace Nickel;

internal sealed class ModStatuses(
	IModManifest modManifest,
	Func<StatusManager> statusManagerProvider
) : IModStatuses
{
	public IReadOnlyDictionary<string, IStatusEntry> RegisteredStatuses
		=> this.RegisteredStatusStorage;
	
	private readonly Dictionary<string, IStatusEntry> RegisteredStatusStorage = [];
	
	public IStatusEntry? LookupByStatus(Status status)
		=> statusManagerProvider().LookupByStatus(status);

	public IStatusEntry? LookupByUniqueName(string uniqueName)
		=> statusManagerProvider().LookupByUniqueName(uniqueName);

	public IStatusEntry RegisterStatus(string name, StatusConfiguration configuration)
	{
		var entry = statusManagerProvider().RegisterStatus(modManifest, name, configuration);
		this.RegisteredStatusStorage[name] = entry;
		return entry;
	}
}

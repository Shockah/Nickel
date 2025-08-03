using System;
using System.Collections.Generic;
using System.Linq;

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

internal sealed class VanillaModStatuses(
	Func<StatusManager> statusManagerProvider
) : IModStatuses
{
	private readonly Lazy<Dictionary<string, IStatusEntry>> LazyRegisteredStatuses = new(() => Enum.GetValues<Status>().Select(d => statusManagerProvider().LookupByStatus(d)!).ToDictionary(e => e.Status.Key()));
	
	public IReadOnlyDictionary<string, IStatusEntry> RegisteredStatuses
		=> this.LazyRegisteredStatuses.Value;
	
	public IStatusEntry? LookupByStatus(Status status)
		=> statusManagerProvider().LookupByStatus(status);

	public IStatusEntry? LookupByUniqueName(string uniqueName)
		=> statusManagerProvider().LookupByUniqueName(uniqueName);

	public IStatusEntry RegisterStatus(string name, StatusConfiguration configuration)
		=> throw new NotSupportedException();
}

namespace Nickel;

/// <summary>
/// A mod-specific status registry.
/// Allows looking up and registering statuses.
/// </summary>
public interface IModStatuses
{
	IStatusEntry? LookupByStatus(Status status);
	IStatusEntry? LookupByUniqueName(string uniqueName);
	IStatusEntry RegisterStatus(string name, StatusConfiguration configuration);
}

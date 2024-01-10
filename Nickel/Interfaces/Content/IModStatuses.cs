namespace Nickel;

public interface IModStatuses
{
	IStatusEntry? LookupByStatus(Status status);
	IStatusEntry? LookupByUniqueName(string uniqueName);
	IStatusEntry RegisterStatus(string name, StatusConfiguration configuration);
}

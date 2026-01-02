namespace Nickel.InfoScreens;

public sealed class InfoScreenEntry(
	IModManifest modOwner,
	string uniqueName,
	string localName,
	Route route,
	double priority
) : IInfoScreensApi.IInfoScreenEntry
{
	public IModManifest ModOwner { get; } = modOwner;
	public string UniqueName { get; } = uniqueName;
	public string LocalName { get; } = localName;
	public IInfoScreensApi.IInfoScreenState State { get; internal set; }
	public double Priority { get; } = priority;
	internal Route Route { get; } = route;

	public void Cancel(G g)
		=> ModEntry.Instance.Cancel(g, this);
}

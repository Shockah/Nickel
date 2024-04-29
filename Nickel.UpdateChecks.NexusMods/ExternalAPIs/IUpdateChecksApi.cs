namespace Nickel.UpdateChecks.NexusMods;

public interface IUpdateChecksApi
{
	void RegisterUpdateSource(string sourceKey, IUpdateSource source);
}

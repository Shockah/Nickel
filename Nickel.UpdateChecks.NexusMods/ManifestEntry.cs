using Newtonsoft.Json;

namespace Nickel.UpdateChecks.NexusMods;

internal sealed class ManifestEntry
{
	[JsonProperty]
	[JsonRequired]
	public int ID { get; internal set; }
}

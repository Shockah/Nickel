using Newtonsoft.Json;

namespace Nickel.UpdateChecks.NexusMods;

internal sealed class ManifestEntry
{
	[JsonProperty("ID")]
	[JsonRequired]
	public int Id { get; internal set; }
}

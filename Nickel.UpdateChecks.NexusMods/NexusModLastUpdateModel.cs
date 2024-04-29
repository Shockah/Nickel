using Newtonsoft.Json;

namespace Nickel.UpdateChecks.NexusMods;

internal sealed class NexusModLastUpdateModel
{
	[JsonProperty("mod_id")]
	[JsonRequired]
	public int ID { get; internal set; }

	[JsonProperty("latest_file_update")]
	[JsonRequired]
	public long LatestFileUpdate { get; internal set; }

	[JsonProperty("latest_mod_activity")]
	[JsonRequired]
	public long LatestModActivity { get; internal set; }
}

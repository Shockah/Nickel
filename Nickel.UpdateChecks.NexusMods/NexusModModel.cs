using Newtonsoft.Json;

namespace Nickel.UpdateChecks.NexusMods;

internal sealed class NexusModModel
{
	[JsonProperty("mod_id")]
	[JsonRequired]
	public int Id { get; internal set; }

	[JsonProperty("version")]
	[JsonRequired]
	public string Version { get; internal set; } = null!;
}

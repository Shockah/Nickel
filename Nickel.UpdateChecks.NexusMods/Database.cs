using Newtonsoft.Json;
using System.Collections.Generic;

namespace Nickel.UpdateChecks.NexusMods;

internal sealed class Database
{
	[JsonProperty]
	public bool IsEnabled = true;

	[JsonProperty]
	public string? ApiKey;

	[JsonProperty]
	public long LastUpdate;

	[JsonProperty]
	public Dictionary<int, SemanticVersion> ModIdToVersion = [];
}

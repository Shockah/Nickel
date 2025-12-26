using Newtonsoft.Json;
using System.Collections.Generic;

namespace Nickel.UpdateChecks;

internal sealed class Settings
{
	[JsonProperty]
	public Dictionary<string, SemanticVersion> IgnoredUpdates = [];
}

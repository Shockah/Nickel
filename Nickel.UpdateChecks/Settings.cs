using Newtonsoft.Json;
using Nickel.Common;
using System.Collections.Generic;

namespace Nickel.UpdateChecks;

internal sealed class Settings
{
	[JsonProperty]
	public Dictionary<string, SemanticVersion> IgnoredUpdates = [];
}

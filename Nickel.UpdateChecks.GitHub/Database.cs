using Newtonsoft.Json;
using Nickel.Common;
using System.Collections.Generic;

namespace Nickel.UpdateChecks.GitHub;

internal sealed class Database
{
	[JsonProperty]
	public bool IsEnabled = true;

	[JsonProperty]
	public string? Token;

	[JsonProperty]
	public long LastUpdate;

	[JsonProperty]
	public Dictionary<string, Entry> UniqueNameToEntry = [];

	internal sealed class Entry
	{
		[JsonProperty]
		[JsonRequired]
		public SemanticVersion Version;

		[JsonProperty]
		[JsonRequired]
		public string Url = null!;
	}
}

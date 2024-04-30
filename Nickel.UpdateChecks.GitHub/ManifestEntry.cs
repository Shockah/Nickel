using Newtonsoft.Json;

namespace Nickel.UpdateChecks.GitHub;

internal sealed class ManifestEntry
{
	[JsonProperty]
	[JsonRequired]
	public string Repository { get; internal set; } = null!;

	[JsonProperty]
	public string? ReleaseTagRegex { get; internal set; }

	[JsonProperty]
	public string? ReleaseNameRegex { get; internal set; }
}

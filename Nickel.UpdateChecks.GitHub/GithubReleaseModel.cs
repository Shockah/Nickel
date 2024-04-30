using Newtonsoft.Json;
using System;

namespace Nickel.UpdateChecks.GitHub;

internal sealed class GithubReleaseModel
{
	[JsonProperty("html_url")]
	[JsonRequired]
	public string Url { get; internal set; } = null!;

	[JsonProperty("tag_name")]
	public string? TagName { get; internal set; }

	[JsonProperty("name")]
	[JsonRequired]
	public string Name { get; internal set; } = null!;

	[JsonProperty("body")]
	public string? Body { get; internal set; }

	[JsonProperty("published_at")]
	public DateTime PublishedAt { get; internal set; }
}

using Newtonsoft.Json;
using Nickel.Common;

namespace Nickel;

public sealed class ModDependency
{
	[JsonProperty]
	[JsonRequired]
	public string UniqueName { get; init; }

	[JsonProperty]
	[JsonConverter(typeof(SemanticVersionConverter))]
	public SemanticVersion? Version { get; init; }

	[JsonProperty]
	public bool IsRequired { get; init; } = true;

	[JsonConstructor]
	private ModDependency()
	{
		this.UniqueName = null!;
	}

	public ModDependency(string uniqueName, SemanticVersion? version = null, bool isRequired = true)
	{
		this.UniqueName = uniqueName;
		this.Version = version;
		this.IsRequired = isRequired;
	}
}

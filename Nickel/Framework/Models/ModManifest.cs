using Newtonsoft.Json;
using Nickel.Common;
using System.Collections.Generic;

namespace Nickel;

internal sealed class ModManifest : IModManifest
{
	[JsonProperty]
	[JsonRequired]
	public string UniqueName { get; internal set; } = null!;

	[JsonProperty]
	[JsonRequired]
	[JsonConverter(typeof(SemanticVersionConverter))]
	public SemanticVersion Version { get; internal set; }

	[JsonProperty]
	[JsonRequired]
	[JsonConverter(typeof(SemanticVersionConverter))]
	public SemanticVersion RequiredApiVersion { get; internal set; }

	[JsonProperty]
	public IReadOnlySet<ModDependency> Dependencies { get; internal set; } = new HashSet<ModDependency>();

	[JsonProperty]
	public string? DisplayName { get; internal set; }

	[JsonProperty]
	public string? Description { get; internal set; }

	[JsonProperty]
	public string? Author { get; internal set; }

	[JsonProperty]
	public string ModType { get; internal set; } = NickelConstants.ModType;

	[JsonProperty]
	public ModLoadPhase LoadPhase { get; internal set; } = ModLoadPhase.AfterGameAssembly;

	[JsonProperty]
	[JsonConverter(typeof(ConcreteTypeConverter<IReadOnlyList<SubmodEntry>>))]
	public IReadOnlyList<ISubmodEntry> Submods { get; internal set; } = new List<ISubmodEntry>();

	[JsonExtensionData]
	public IDictionary<string, object> ExtensionData { get; internal set; } = new Dictionary<string, object>();

	IReadOnlyDictionary<string, object> IModManifest.ExtensionData
		=> (IReadOnlyDictionary<string, object>)this.ExtensionData;
}

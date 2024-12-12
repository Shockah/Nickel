using Newtonsoft.Json;
using Nickel.Common;
using System.Collections.Generic;

namespace Nickel.Legacy;

internal sealed class GeneratedLegacyModManifest : IAssemblyModManifest
{
	[JsonProperty]
	public required string EntryPointAssembly { get; init; }

	[JsonProperty]
	public required string UniqueName { get; init; }

	[JsonProperty]
	[JsonConverter(typeof(SemanticVersionConverter))]
	public required SemanticVersion Version { get; init; }

	[JsonProperty]
	[JsonConverter(typeof(SemanticVersionConverter))]
	public required SemanticVersion? RequiredApiVersion { get; init; }

	[JsonProperty]
	public required IReadOnlySet<ModDependency> Dependencies { get; init; }

	[JsonProperty]
	public string ModType { get; } = ModEntry.LegacyModType;

	[JsonIgnore]
	public ModLoadPhase LoadPhase
		=> ModLoadPhase.AfterGameAssembly;

	[JsonIgnore]
	public string? DisplayName
		=> null;

	[JsonIgnore]
	public string? Description
		=> null;

	[JsonIgnore]
	public string? Author
		=> null;

	[JsonIgnore]
	public string? EntryPointType
		=> null;

	[JsonIgnore]
	public IReadOnlyList<ModAssemblyReference> AssemblyReferences { get; } = [];

	[JsonIgnore]
	public IReadOnlyList<ISubmodEntry> Submods { get; } = [];

	[JsonExtensionData]
	public IDictionary<string, object> ExtensionData { get; } = new Dictionary<string, object>();

	IReadOnlyDictionary<string, object> IModManifest.ExtensionData
		=> (IReadOnlyDictionary<string, object>)this.ExtensionData;
}

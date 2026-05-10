using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class AssemblyModManifest : IAssemblyModManifest
{
	#region IModManifest
	
	[JsonProperty]
	[JsonRequired]
	public string UniqueName { get; internal set; } = null!;

	[JsonProperty]
	[JsonRequired]
	[JsonConverter(typeof(SemanticVersionConverter))]
	public SemanticVersion Version { get; internal set; }

	[JsonProperty]
	public IReadOnlySet<ModDependency> Dependencies { get; internal set; } = new HashSet<ModDependency>();

	[JsonProperty]
	[JsonConverter(typeof(SemanticVersionConverter))]
	public SemanticVersion? MinimumGameVersion { get; internal set; }

	[JsonProperty]
	[JsonConverter(typeof(SemanticVersionConverter))]
	public SemanticVersion? UnsupportedGameVersion { get; internal set; }

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
	
	[JsonProperty]
	public IReadOnlyList<StopInliningDefinition> MethodsToStopInlining { get; internal set; } = [];

	[JsonExtensionData]
	public IDictionary<string, object> ExtensionData { get; set; } = new Dictionary<string, object>();

	IReadOnlyDictionary<string, object> IModManifest.ExtensionData
		=> (IReadOnlyDictionary<string, object>)this.ExtensionData;
	
	#endregion

	public string EntryPointAssembly { get; internal set; } = null!;

	public string? EntryPointType { get; internal set; }
	
	[JsonProperty]
	[JsonConverter(typeof(SemanticVersionConverter))]
	public SemanticVersion? RequiredApiVersion { get; internal set; }

	public IReadOnlyList<ModAssemblyReference> AssemblyReferences { get; internal set; } = [];

	public static AssemblyModManifest From(IModManifest modManifest)
		=> new()
		{
			UniqueName = modManifest.UniqueName,
			Version = modManifest.Version,
			Dependencies = modManifest.Dependencies,
			MinimumGameVersion = modManifest.MinimumGameVersion,
			UnsupportedGameVersion = modManifest.UnsupportedGameVersion,
			DisplayName = modManifest.DisplayName,
			Description = modManifest.Description,
			Author = modManifest.Author,
			ModType = modManifest.ModType,
			LoadPhase = modManifest.LoadPhase,
			Submods = modManifest.Submods,
			MethodsToStopInlining = modManifest.MethodsToStopInlining,
			
			ExtensionData = modManifest.ExtensionData
				.Where(kvp => kvp.Key != nameof(EntryPointAssembly))
				.Where(kvp => kvp.Key != nameof(EntryPointType))
				.Where(kvp => kvp.Key != nameof(RequiredApiVersion))
				.Where(kvp => kvp.Key != nameof(AssemblyReferences))
				.ToDictionary()
		};
}

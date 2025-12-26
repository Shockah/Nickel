using Newtonsoft.Json;

namespace Nickel;

/// <summary>
/// Describes an inter-mod dependency.
/// </summary>
public sealed class ModDependency
{
	/// <summary>The <see cref="IModManifest.UniqueName"/> of the dependency.</summary>
	[JsonProperty]
	[JsonRequired]
	public string UniqueName { get; init; }

	/// <summary>The minimum version of the dependency.</summary>
	[JsonProperty]
	[JsonConverter(typeof(SemanticVersionConverter))]
	public SemanticVersion? Version { get; init; }

	/// <summary>
	/// Whether the dependency is required.<br/>
	/// All dependencies (including optional ones) will be loaded before the dependent mod.
	/// </summary>
	[JsonProperty]
	public bool IsRequired { get; init; } = true;

	[JsonConstructor]
	private ModDependency()
	{
		this.UniqueName = null!;
	}

	/// <summary>
	/// Creates a new instance of <see cref="ModDependency"/>.
	/// </summary>
	/// <param name="uniqueName">The <see cref="IModManifest.UniqueName"/> of the dependency.</param>
	/// <param name="version">The minimum version of the dependency.</param>
	/// <param name="isRequired">Whether the dependency is required.</param>
	public ModDependency(string uniqueName, SemanticVersion? version = null, bool isRequired = true)
	{
		this.UniqueName = uniqueName;
		this.Version = version;
		this.IsRequired = isRequired;
	}
}

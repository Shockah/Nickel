using Newtonsoft.Json;

namespace Nickel;

/// <summary>
/// Describes a mod's assembly (DLL) reference.
/// </summary>
public sealed class ModAssemblyReference
{
	/// <summary>The assembly name (not the file name).</summary>
	public string Name { get; init; }

	/// <summary>
	/// Whether the assembly should be shared between all mods (if <c>true</c>), or loaded into the mod's private context (if <c>false</c>).<br/>
	/// A shared assembly can only be loaded once, but can be easily referenced between mods.<br/>
	/// A private assembly can be loaded for each mod separately, even at different versions, but their objects can't be easily shared.
	/// </summary>
	public bool IsShared { get; init; } = true;

	[JsonConstructor]
	private ModAssemblyReference()
	{
		this.Name = null!;
	}

	/// <summary>
	/// Creates a new instance of <see cref="ModAssemblyReference"/>.
	/// </summary>
	/// <param name="name">The assembly name (not the file name).</param>
	/// <param name="isShared">
	/// Whether the assembly should be shared between all mods.<br/>
	/// See also: <seealso cref="IsShared"/>
	/// </param>
	public ModAssemblyReference(string name, bool isShared = true)
	{
		this.Name = name;
		this.IsShared = isShared;
	}
}

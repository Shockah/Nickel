using System.Text.Json.Serialization;

namespace Nickel;

public sealed class ModAssemblyReference
{
	public string Name { get; init; }
	public bool IsShared { get; init; } = true;

	[JsonConstructor]
	private ModAssemblyReference()
	{
		this.Name = null!;
	}

	public ModAssemblyReference(string name, bool isShared = true)
	{
		this.Name = name;
		this.IsShared = isShared;
	}
}

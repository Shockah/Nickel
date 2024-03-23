using System.Collections.Generic;

namespace Nickel;

/// <summary>
/// Describes an assembly/code mod's manifest file.
/// </summary>
public interface IAssemblyModManifest : IModManifest
{
	/// <summary>The assembly file name to load as the entry point.</summary>
	string EntryPointAssembly { get; }

	/// <summary>
	/// An optional type name to load as the entry point.<br/>
	/// If not provided, the mod loader will try to automatically find a singular <see cref="Mod"/> subclass.
	/// </summary>
	string? EntryPointType { get; }

	/// <summary>The mod's assembly (DLL) references.</summary>
	IReadOnlyList<ModAssemblyReference> AssemblyReferences { get; }
}

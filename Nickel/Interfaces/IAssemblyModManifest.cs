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

	/// <summary>The minimum version of the mod loader that this mod will load in.</summary>
	SemanticVersion? RequiredApiVersion { get; }

	/// <summary>The mod's assembly (DLL) references.</summary>
	IReadOnlyList<ModAssemblyReference> AssemblyReferences { get; }
	
	/// <summary>
	/// Describes methods that Nickel should attempt to stop from getting inlined.<br/>
	/// https://harmony.pardeike.net/articles/patching-edgecases.html#inlining
	/// </summary>
	IReadOnlyList<StopInliningDefinition> MethodsToStopInlining { get; }
}

using System.Collections.Generic;

namespace Nickel;

public interface IAssemblyModManifest : IModManifest
{
	string EntryPointAssembly { get; }
	string? EntryPointType { get; }
	IReadOnlyList<ModAssemblyReference> AssemblyReferences { get; }
}

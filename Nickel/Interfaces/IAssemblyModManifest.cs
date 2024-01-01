namespace Nickel;

public interface IAssemblyModManifest : IModManifest
{
	string EntryPointAssembly { get; }
	string? EntryPointType { get; }
	ModLoadPhase LoadPhase { get; }
}

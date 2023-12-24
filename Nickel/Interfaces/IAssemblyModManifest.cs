namespace Nickel;

public interface IAssemblyModManifest : IModManifest
{
    string EntryPointAssemblyFileName { get; }
    ModLoadPhase LoadPhase { get; }
}

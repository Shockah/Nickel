namespace Nanoray.PluginManager;

public readonly struct AssemblyPluginLoaderRequiredPluginData
{
	public string UniqueName { get; init; }
	public string EntryPointAssembly { get; init; }
	public string? EntryPointType { get; init; }
}

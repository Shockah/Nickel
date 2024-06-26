namespace Nanoray.PluginManager;

/// <summary>
/// Describes data required for loading plugins via the <see cref="AssemblyPluginLoader{TPluginManifest,TPluginPart,TPlugin}"/>.
/// </summary>
public readonly struct AssemblyPluginLoaderRequiredPluginData
{
	/// <summary>The unique name of the plugin.</summary>
	public string UniqueName { get; init; }
	
	/// <summary>The name of the entry point assembly.</summary>
	public string EntryPointAssembly { get; init; }
	
	/// <summary>The name of the entry point type. If left <c>null</c>, <see cref="AssemblyPluginLoader{TPluginManifest,TPluginPart,TPlugin}"/> will try to resolve one automatically.</summary>
	public string? EntryPointType { get; init; }
}

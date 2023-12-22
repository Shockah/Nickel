namespace Nickel;

public static partial class ManifestExt
{
    public static IAssemblyModManifest? AsAssemblyModManifest(this IModManifest manifest)
    {
        if (!manifest.ExtensionData.TryGetValue("EntryPointAssemblyFileName", out object? rawEntryPointAssemblyFileName) || rawEntryPointAssemblyFileName is not string entryPointAssemblyFileName)
            return null;
        return new AssemblyModManifest(manifest) { EntryPointAssemblyFileName = entryPointAssemblyFileName };
    }
}

using System.Collections.Generic;
using System.IO;
using OneOf.Types;
using OneOf;

namespace Shockah.PluginManager;

public sealed class RecursiveDirectoryPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
    public DirectoryInfo Directory { get; private init; }
    public string ManifestFileName { get; private init; }
    private IPluginManifestLoader<TPluginManifest> PluginManifestLoader { get; init; }

    public RecursiveDirectoryPluginPackageResolver(DirectoryInfo directory, string manifestFileName, IPluginManifestLoader<TPluginManifest> pluginManifestLoader)
    {
        this.Directory = directory;
        this.ManifestFileName = manifestFileName;
        this.PluginManifestLoader = pluginManifestLoader;
    }

    public IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages()
    {
        // not resolving in the main folder on purpose, only in child folders
        foreach (var childDirectory in this.Directory.GetDirectories())
            foreach (var package in this.Resolve(childDirectory))
                yield return package;
    }

    private IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> Resolve(DirectoryInfo directory)
    {
        foreach (var package in new DirectoryPluginPackageResolver<TPluginManifest>(directory, this.ManifestFileName, this.PluginManifestLoader).ResolvePluginPackages())
            yield return package;

        // stop recursing if there is a manifest file
        FileInfo manifestFile = new(Path.Combine(this.Directory.FullName, this.ManifestFileName));
        if (manifestFile.Exists)
            yield break;

        foreach (var childDirectory in directory.GetDirectories())
            foreach (var package in this.Resolve(childDirectory))
                yield return package;
    }
}

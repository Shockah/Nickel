using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IPluginLoader{TPluginManifest,TPlugin}"/> which copies all of the package files to the given directory before attempting to load the package.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TPlugin">The plugin type.</typeparam>
public sealed class CopyingPluginLoader<TPluginManifest, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
{
	private readonly IPluginLoader<TPluginManifest, TPlugin> Loader;
	private readonly Func<IPluginPackage<TPluginManifest>, IWritableDirectoryInfo?> ExtractedDirectoryProvider;
	private readonly Dictionary<IPluginPackage<TPluginManifest>, IPluginPackage<TPluginManifest>> ExtractedPackages = [];
	
	/// <summary>
	/// Creates a new <see cref="CopyingPluginLoader{TPluginManifest,TPlugin}"/>.
	/// </summary>
	/// <param name="loader">The underlying loader.</param>
	/// <param name="extractedDirectoryProvider">A function which provides the directory path that should hold the copied plugin files.</param>
	public CopyingPluginLoader(
		IPluginLoader<TPluginManifest, TPlugin> loader,
		Func<IPluginPackage<TPluginManifest>, IWritableDirectoryInfo?> extractedDirectoryProvider
	)
	{
		this.Loader = loader;
		this.ExtractedDirectoryProvider = extractedDirectoryProvider;
	}

	/// <inheritdoc/>
	public OneOf<Yes, No, Error<string>> CanLoadPlugin(IPluginPackage<TPluginManifest> package)
		=> this.Loader.CanLoadPlugin(this.ObtainExtractedPackage(package));

	/// <inheritdoc/>
	public PluginLoadResult<TPlugin> LoadPlugin(IPluginPackage<TPluginManifest> package)
		=> this.Loader.LoadPlugin(this.ObtainExtractedPackage(package));

	private IPluginPackage<TPluginManifest> ObtainExtractedPackage(IPluginPackage<TPluginManifest> package)
	{
		if (this.ExtractedPackages.TryGetValue(package, out var extractedPackage))
			return extractedPackage;
		if (this.ExtractedDirectoryProvider(package) is not { } extractedDirectory)
			return package;
		
		foreach (var toExtractFile in package.PackageRoot.GetFilesRecursively())
		{
			var extractedFile = extractedDirectory.GetRelativeFile(package.PackageRoot.GetRelativePathTo(toExtractFile));
			using var readStream = toExtractFile.OpenRead();
			using var writeStream = extractedFile.OpenWrite();
			readStream.CopyTo(writeStream);
		}

		extractedPackage = new DirectoryPluginPackage<TPluginManifest>(package.Manifest, extractedDirectory);
		this.ExtractedPackages[package] = extractedPackage;
		return extractedPackage;
	}
}

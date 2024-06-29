using OneOf.Types;
using System;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IPluginPackageResolver{TPluginManifest}"/> which resolves plugins from a directory recursively.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
public sealed class RecursiveDirectoryPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
	private IDirectoryInfo Directory { get; }
	private string ManifestFileName { get; }
	private bool IgnoreDotNames { get; }
	private bool AllowPluginsInRoot { get; }
	private Func<IDirectoryInfo, IPluginPackageResolver<TPluginManifest>?>? DirectoryResolverFactory { get; }
	private Func<IFileInfo, IPluginPackageResolver<TPluginManifest>?>? FileResolverFactory { get; }

	/// <summary>
	/// Creates a new <see cref="RecursiveDirectoryPluginPackageResolver{TPluginManifest}"/>.
	/// </summary>
	/// <param name="directory">The directory</param>
	/// <param name="manifestFileName">The manifest file name to look for.</param>
	/// <param name="ignoreDotNames">Whether to ignore directory names starting with a dot.</param>
	/// <param name="allowPluginsInRoot">Whether plugins placed directly in the root directory should be allowed.</param>
	/// <param name="directoryResolverFactory">A function providing a plugin package resolver for a given directory.</param>
	/// <param name="fileResolverFactory">A function providing a plugin package resolver for a given file.</param>
	public RecursiveDirectoryPluginPackageResolver(
		IDirectoryInfo directory,
		string manifestFileName,
		bool ignoreDotNames,
		bool allowPluginsInRoot,
		Func<IDirectoryInfo, IPluginPackageResolver<TPluginManifest>?>? directoryResolverFactory,
		Func<IFileInfo, IPluginPackageResolver<TPluginManifest>?>? fileResolverFactory
	)
	{
		this.Directory = directory;
		this.ManifestFileName = manifestFileName;
		this.IgnoreDotNames = ignoreDotNames;
		this.AllowPluginsInRoot = allowPluginsInRoot;
		this.DirectoryResolverFactory = directoryResolverFactory;
		this.FileResolverFactory = fileResolverFactory;
	}

	/// <inheritdoc/>
	public IEnumerable<PluginPackageResolveResult<TPluginManifest>> ResolvePluginPackages()
	{
		if (this.DirectoryResolverFactory is null && this.FileResolverFactory is null)
			return [];
		return this.ResolvePluginPackages(this.Directory, isRoot: true);
	}

	private IEnumerable<PluginPackageResolveResult<TPluginManifest>> ResolvePluginPackages(IDirectoryInfo directory, bool isRoot = false)
	{
		var manifestFile = directory.GetRelativeFile(this.ManifestFileName);
		if (manifestFile.Exists)
		{
			if (isRoot && !this.AllowPluginsInRoot)
			{
				yield return new Error<string>($"Found a manifest file at `{manifestFile.FullName}`, but it's in the root of the mods folder.");
				yield break;
			}

			if (this.DirectoryResolverFactory?.Invoke(directory) is { } directoryResolver)
				foreach (var package in directoryResolver.ResolvePluginPackages())
					yield return package;
			yield break;
		}

		foreach (var file in directory.Files)
			if ((!file.Name.StartsWith('.') || !this.IgnoreDotNames) && this.FileResolverFactory?.Invoke(file) is { } fileResolver)
				foreach (var package in fileResolver.ResolvePluginPackages())
					yield return package;

		foreach (var childDirectory in directory.Directories)
		{
			if (childDirectory.Name.StartsWith('.') && this.IgnoreDotNames)
				continue;

			var hadAnyChildPackages = false;
			foreach (var package in this.ResolvePluginPackages(childDirectory))
			{
				hadAnyChildPackages = true;
				yield return package;
			}

			if (isRoot && !this.AllowPluginsInRoot && !hadAnyChildPackages)
				yield return new Error<string>($"Found a folder `{childDirectory.FullName}`, but it does not contain any mods.");
		}
	}
}

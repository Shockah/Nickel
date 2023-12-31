using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

public sealed class RecursiveDirectoryPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
	private IDirectoryInfo Directory { get; }
	private string ManifestFileName { get; }
	private bool IgnoreDotNames { get; }
	private bool AllowPluginsInRoot { get; }
	private Func<IDirectoryInfo, IPluginPackageResolver<TPluginManifest>?>? DirectoryResolverFactory { get; }
	private Func<IFileInfo, IPluginPackageResolver<TPluginManifest>?>? FileResolverFactory { get; }

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

	public IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages()
	{
		if (this.DirectoryResolverFactory is null && this.FileResolverFactory is null)
			return [];
		return this.ResolvePluginPackages(this.Directory, isRoot: true);
	}

	private IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages(IDirectoryInfo directory, bool isRoot = false)
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
			if ((!file.Name.StartsWith(".") || !this.IgnoreDotNames) && this.FileResolverFactory?.Invoke(file) is { } fileResolver)
				foreach (var package in fileResolver.ResolvePluginPackages())
					yield return package;

		foreach (var childDirectory in directory.Directories)
		{
			if (childDirectory.Name.StartsWith(".") && this.IgnoreDotNames)
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

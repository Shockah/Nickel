using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.IO.Compression;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IPluginPackageResolver{TPluginManifest}"/> which resolves plugins from a ZIP file.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
public sealed class ZipPluginPackageResolver<TPluginManifest> : IPluginPackageResolver<TPluginManifest>
{
	private IFileInfo ZipFile { get; }
	private Func<IDirectoryInfo, IPluginPackageResolver<TPluginManifest>> ResolverFactory { get; }

	/// <summary>
	/// Creates a new <see cref="ZipPluginPackageResolver{TPluginManifest}"/>
	/// </summary>
	/// <param name="zipFile">The ZIP file to resolve plugins from.</param>
	/// <param name="resolverFactory">A function providing a plugin package resolver for a given directory.</param>
	public ZipPluginPackageResolver(
		IFileInfo zipFile,
		Func<IDirectoryInfo, IPluginPackageResolver<TPluginManifest>> resolverFactory
	)
	{
		this.ZipFile = zipFile;
		this.ResolverFactory = resolverFactory;
	}

	/// <inheritdoc/>
	public IEnumerable<OneOf<IPluginPackage<TPluginManifest>, Error<string>>> ResolvePluginPackages()
	{
		if (!this.ZipFile.Exists)
		{
			yield return new Error<string>($"Could not find a ZIP file at `{this.ZipFile.FullName}`.");
			yield break;
		}

		ZipArchive archive = new(this.ZipFile.OpenRead(), ZipArchiveMode.Read, leaveOpen: true);
		foreach (var packageOrError in this.ResolverFactory(ZipDirectoryInfo.From(archive)).ResolvePluginPackages())
			yield return packageOrError.Match<OneOf<IPluginPackage<TPluginManifest>, Error<string>>>(
				package => new InnerPluginPackage<TPluginManifest>(package, package.Manifest, disposesOuterPackage: false),
				error => error
			);
	}
}

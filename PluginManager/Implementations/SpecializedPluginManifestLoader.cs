using OneOf;
using OneOf.Types;
using System.IO;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IPluginManifestLoader{TPluginManifest}"/> which loads manifests of a specialized subclass.
/// </summary>
/// <typeparam name="TSpecializedPluginManifest">The specialized type of the plugin manifest.</typeparam>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
public sealed class SpecializedPluginManifestLoader<TSpecializedPluginManifest, TPluginManifest> : IPluginManifestLoader<TPluginManifest>
	where TSpecializedPluginManifest : TPluginManifest
{
	private readonly IPluginManifestLoader<TSpecializedPluginManifest> ManifestLoader;

	/// <summary>
	/// Creates a new <see cref="SpecializedPluginManifestLoader{TSpecializedPluginManifest,TPluginManifest}"/>.
	/// </summary>
	/// <param name="manifestLoader">The underlying plugin manifest loader.</param>
	public SpecializedPluginManifestLoader(IPluginManifestLoader<TSpecializedPluginManifest> manifestLoader)
	{
		this.ManifestLoader = manifestLoader;
	}

	/// <inheritdoc/>
	public OneOf<TPluginManifest, Error<string>> LoadPluginManifest(Stream stream)
		=> this.ManifestLoader.LoadPluginManifest(stream).Match<OneOf<TPluginManifest, Error<string>>>(
			manifest => manifest,
			error => error
		);
}

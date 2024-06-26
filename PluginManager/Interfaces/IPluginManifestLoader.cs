using OneOf;
using OneOf.Types;
using System.IO;

namespace Nanoray.PluginManager;

/// <summary>
/// A type that loads plugin manifests from <see cref="Stream"/> instances.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
public interface IPluginManifestLoader<TPluginManifest>
{
	/// <summary>
	/// Attemps to load a plugin manifest from the given <see cref="Stream"/>.
	/// </summary>
	/// <param name="stream">The stream.</param>
	/// <returns>A plugin manifest, or an error.</returns>
	OneOf<TPluginManifest, Error<string>> LoadPluginManifest(Stream stream);
}

using Newtonsoft.Json;
using OneOf;
using OneOf.Types;
using System.IO;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IPluginManifestLoader{TPluginManifest}"/> which loads manifests from JSON files.
/// </summary>
/// <typeparam name="TPluginManifest"></typeparam>
public sealed class JsonPluginManifestLoader<TPluginManifest> : IPluginManifestLoader<TPluginManifest>
{
	private readonly JsonSerializer Serializer = new();

	/// <inheritdoc/>
	public OneOf<TPluginManifest, Error<string>> LoadPluginManifest(Stream stream)
	{
		using StreamReader reader = new(stream);
		using JsonTextReader jsonReader = new(reader);
		var manifest = this.Serializer.Deserialize<TPluginManifest>(jsonReader);
		if (manifest is null)
			return new Error<string>($"The provided data could not be deserialized as `{typeof(TPluginManifest)}`.");
		return manifest;
	}
}

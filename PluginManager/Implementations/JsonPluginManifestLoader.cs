using Newtonsoft.Json;
using OneOf;
using OneOf.Types;
using System.IO;

namespace Nanoray.PluginManager;

public sealed class JsonPluginManifestLoader<TPluginManifest> : IPluginManifestLoader<TPluginManifest>
{
	private JsonSerializer Serializer { get; } = new();

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

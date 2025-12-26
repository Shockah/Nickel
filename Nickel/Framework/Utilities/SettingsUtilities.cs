using Nanoray.PluginManager;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;

namespace Nickel;

internal static class SettingsUtilities
{
	public static T? ReadSettings<T>(IWritableDirectoryInfo modStorageDirectory, bool writeOnSuccess) where T : class, new()
	{
		var serializerSettings = new JsonSerializerSettings { Formatting = Formatting.Indented };
		serializerSettings.Converters.Add(new StringEnumConverter());
		serializerSettings.Converters.Add(new SemanticVersionConverter());
		var serializer = JsonSerializer.Create(serializerSettings);

		var settings = new T();
		var settingsFile = modStorageDirectory.GetRelativeFile("Nickel.json");
		if (settingsFile.Exists)
		{
			using var stream = settingsFile.OpenRead();
			using var streamReader = new StreamReader(stream);
			using var jsonReader = new JsonTextReader(streamReader);
			settings = serializer.Deserialize<T>(jsonReader);
		}
		if (writeOnSuccess && settings is not null)
		{
			using var stream = settingsFile.OpenWrite();
			using var streamWriter = new StreamWriter(stream);
			using var jsonWriter = new JsonTextWriter(streamWriter);
			serializer.Serialize(jsonWriter, settings);
		}

		return settings;
	}
}

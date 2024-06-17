using Nanoray.PluginManager;
using Newtonsoft.Json;
using System;

namespace Nickel;

public interface IModStorage
{
	IWritableDirectoryInfo SettingsDirectory { get; }
	IWritableFileInfo GetSingleSettingsFile(string fileExtension);
	IWritableDirectoryInfo PrivateSettingsDirectory { get; }
	IWritableFileInfo GetSinglePrivateSettingsFile(string fileExtension);

	void ApplyGlobalJsonSerializerSettings(Action<JsonSerializerSettings> function, double priority = 0);
	void ApplyJsonSerializerSettings(Action<JsonSerializerSettings> function, double priority = 0);
	JsonSerializerSettings JsonSerializerSettings { get; }
	JsonSerializer JsonSerializer { get; }
}

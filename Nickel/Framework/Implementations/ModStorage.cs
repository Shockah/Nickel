using Nanoray.PluginManager;
using Newtonsoft.Json;
using System;

namespace Nickel;

internal sealed class ModStorage : IModStorage
{
	private readonly IModManifest ModManifest;
	private readonly IWritableDirectoryInfo CommonSettingsDirectory;
	private readonly IWritableDirectoryInfo CommonPrivateSettingsDirectory;
	private ModStorageManager ModStorageManager { get; }

	public ModStorage(IModManifest modManifest, IWritableDirectoryInfo commonSettingsDirectory, IWritableDirectoryInfo commonPrivateSettingsDirectory, ModStorageManager modStorageManager)
	{
		this.ModManifest = modManifest;
		this.CommonSettingsDirectory = commonSettingsDirectory;
		this.CommonPrivateSettingsDirectory = commonPrivateSettingsDirectory;
		this.ModStorageManager = modStorageManager;
	}

	public IWritableDirectoryInfo SettingsDirectory
		=> this.CommonSettingsDirectory.GetRelativeDirectory(this.ModManifest.UniqueName);

	public IWritableFileInfo GetSingleSettingsFile(string fileExtension)
		=> this.CommonSettingsDirectory.GetRelativeFile($"{this.ModManifest.UniqueName}.{fileExtension}");

	public IWritableDirectoryInfo PrivateSettingsDirectory
		=> this.CommonPrivateSettingsDirectory.GetRelativeDirectory(this.ModManifest.UniqueName);

	public IWritableFileInfo GetSinglePrivateSettingsFile(string fileExtension)
		=> this.CommonPrivateSettingsDirectory.GetRelativeFile($"{this.ModManifest.UniqueName}.{fileExtension}");

	public void ApplyGlobalJsonSerializerSettings(Action<JsonSerializerSettings> function, double priority = 0)
		=> this.ModStorageManager.ApplyGlobalJsonSerializerSettings(function, priority);

	public void ApplyJsonSerializerSettings(Action<JsonSerializerSettings> function, double priority = 0)
		=> this.ModStorageManager.ApplyJsonSerializerSettingsForMod(this.ModManifest, function, priority);

	public JsonSerializerSettings JsonSerializerSettings
		=> this.ModStorageManager.GetSerializerSettingsForMod(this.ModManifest);

	public JsonSerializer JsonSerializer
		=> this.ModStorageManager.GetSerializerForMod(this.ModManifest);
}

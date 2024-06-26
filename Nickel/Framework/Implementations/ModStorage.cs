using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Nickel;

internal sealed class ModStorage : IModStorage
{
	private readonly IModManifest ModManifest;
	private readonly Func<ILogger> LoggerProvider;
	private readonly IWritableDirectoryInfo CommonSettingsDirectory;
	private readonly IWritableDirectoryInfo CommonPrivateSettingsDirectory;
	private ModStorageManager ModStorageManager { get; }

	public ModStorage(IModManifest modManifest, Func<ILogger> loggerProvider, IWritableDirectoryInfo commonSettingsDirectory, IWritableDirectoryInfo commonPrivateSettingsDirectory, ModStorageManager modStorageManager)
	{
		this.ModManifest = modManifest;
		this.LoggerProvider = loggerProvider;
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

	public bool TryLoadJson<T>(IFileInfo file, [MaybeNullWhen(false)] out T obj)
	{
		try
		{
			if (!file.Exists)
			{
				obj = default;
				return false;
			}
			
			using var stream = file.OpenRead();
			using var streamReader = new StreamReader(stream);
			using var jsonReader = new JsonTextReader(streamReader);

			obj = this.JsonSerializer.Deserialize<T>(jsonReader);
			if (obj is null)
			{
				this.LoggerProvider().LogWarning("Failed to load the JSON settings file {file}.", file);
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			this.LoggerProvider().LogWarning("Failed to load the JSON settings file `{file}`: {ex}", file, ex);
			obj = default;
			return false;
		}
	}

	public void SaveJson<T>(IWritableFileInfo file, T obj)
	{
		try
		{
			using var stream = file.OpenWrite();
			using var streamWriter = new StreamWriter(stream);
			using var jsonWriter = new JsonTextWriter(streamWriter);
			this.JsonSerializer.Serialize(jsonWriter, obj);
		}
		catch (Exception ex)
		{
			this.LoggerProvider().LogWarning("Failed to save the JSON settings file `{file}`: {ex}", file, ex);
		}
	}
}

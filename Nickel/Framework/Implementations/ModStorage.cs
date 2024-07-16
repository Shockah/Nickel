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
	private readonly IWritableDirectoryInfo CommonStorageDirectory;
	private readonly IWritableDirectoryInfo CommonPrivateStorageDirectory;
	private readonly ModStorageManager ModStorageManager;

	public ModStorage(IModManifest modManifest, Func<ILogger> loggerProvider, IWritableDirectoryInfo commonStorageDirectory, IWritableDirectoryInfo commonPrivateStorageDirectory, ModStorageManager modStorageManager)
	{
		this.ModManifest = modManifest;
		this.LoggerProvider = loggerProvider;
		this.CommonStorageDirectory = commonStorageDirectory;
		this.CommonPrivateStorageDirectory = commonPrivateStorageDirectory;
		this.ModStorageManager = modStorageManager;
	}

	public IWritableDirectoryInfo StorageDirectory
		=> this.CommonStorageDirectory.GetRelativeDirectory(this.ModManifest.UniqueName);

	public IWritableFileInfo GetMainStorageFile(string fileExtension)
		=> this.CommonStorageDirectory.GetRelativeFile($"{this.ModManifest.UniqueName}.{fileExtension}");

	public IWritableDirectoryInfo PrivateStorageDirectory
		=> this.CommonPrivateStorageDirectory.GetRelativeDirectory(this.ModManifest.UniqueName);

	public IWritableFileInfo GetMainPrivateStorageFile(string fileExtension)
		=> this.CommonPrivateStorageDirectory.GetRelativeFile($"{this.ModManifest.UniqueName}.{fileExtension}");

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

using Microsoft.Extensions.Logging;
using Nanoray.PluginManager;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Nickel;

internal sealed class ModStorage(
	IModManifest modManifest,
	Func<ILogger> loggerProvider,
	IWritableDirectoryInfo commonStorageDirectory,
	IWritableDirectoryInfo commonPrivateStorageDirectory,
	ModStorageManager modStorageManager
) : IModStorage
{
	public IWritableDirectoryInfo StorageDirectory
		=> commonStorageDirectory.GetRelativeDirectory(modManifest.UniqueName);

	public IWritableFileInfo GetMainStorageFile(string fileExtension)
		=> commonStorageDirectory.GetRelativeFile($"{modManifest.UniqueName}.{fileExtension}");

	public IWritableDirectoryInfo PrivateStorageDirectory
		=> commonPrivateStorageDirectory.GetRelativeDirectory(modManifest.UniqueName);

	public IWritableFileInfo GetMainPrivateStorageFile(string fileExtension)
		=> commonPrivateStorageDirectory.GetRelativeFile($"{modManifest.UniqueName}.{fileExtension}");

	public void ApplyGlobalJsonSerializerSettings(Action<JsonSerializerSettings> function, double priority = 0)
		=> modStorageManager.ApplyGlobalJsonSerializerSettings(function, priority);

	public void ApplyJsonSerializerSettings(Action<JsonSerializerSettings> function, double priority = 0)
		=> modStorageManager.ApplyJsonSerializerSettingsForMod(modManifest, function, priority);

	public JsonSerializerSettings JsonSerializerSettings
		=> modStorageManager.GetSerializerSettingsForMod(modManifest);

	public JsonSerializer JsonSerializer
		=> modStorageManager.GetSerializerForMod(modManifest);

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
				loggerProvider().LogWarning("Failed to load the JSON settings file {file}.", file);
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			loggerProvider().LogWarning("Failed to load the JSON settings file `{file}`: {ex}", file, ex);
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
			loggerProvider().LogWarning("Failed to save the JSON settings file `{file}`: {ex}", file, ex);
		}
	}
}

internal sealed class VanillaModStorage(
	IModManifest modManifest,
	ModStorageManager modStorageManager
) : IModStorage
{
	public IWritableDirectoryInfo StorageDirectory
		=> throw new NotSupportedException();
	
	public IWritableFileInfo GetMainStorageFile(string fileExtension)
		=> throw new NotSupportedException();

	public IWritableDirectoryInfo PrivateStorageDirectory
		=> throw new NotSupportedException();
	
	public IWritableFileInfo GetMainPrivateStorageFile(string fileExtension)
		=> throw new NotSupportedException();

	public void ApplyGlobalJsonSerializerSettings(Action<JsonSerializerSettings> function, double priority = 0)
		=> throw new NotSupportedException();

	public void ApplyJsonSerializerSettings(Action<JsonSerializerSettings> function, double priority = 0)
		=> throw new NotSupportedException();

	public JsonSerializerSettings JsonSerializerSettings
		=> modStorageManager.GetSerializerSettingsForMod(modManifest);

	public JsonSerializer JsonSerializer
		=> modStorageManager.GetSerializerForMod(modManifest);
	
	public bool TryLoadJson<T>(IFileInfo file, [MaybeNullWhen(false)] out T obj)
		=> throw new NotSupportedException();

	public void SaveJson<T>(IWritableFileInfo file, T obj)
		=> throw new NotSupportedException();
}

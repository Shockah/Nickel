using Nanoray.PluginManager;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

/// <summary>
/// A mod-specific storage manager.<br/>
/// Allows mods to save and load arbitrary files within common save file directories.
/// </summary>
public interface IModStorage
{
	/// <summary>
	/// Gets a path to a writable subdirectory specific for this mod, within the directory that should be used to store mod settings.
	/// This directory is usually stored next to the save files.
	/// </summary>
	IWritableDirectoryInfo SettingsDirectory { get; }
	
	/// <summary>
	/// Get a path to a writable file specific for this mod, within the directory that should be used to store mod settings.
	/// This directory is usually stored next to the save files.
	/// </summary>
	/// <param name="fileExtension">The extension of the settings file (usually <c>json</c>).</param>
	/// <returns>The path to a writable file.</returns>
	IWritableFileInfo GetSingleSettingsFile(string fileExtension);
	
	/// <summary>
	/// Gets a path to a writable subdirectory specific for this mod, within the directory that should be used to store private mod settings.
	/// This directory is usually stored in the user's application data folder and is not meant to be shared.
	/// </summary>
	IWritableDirectoryInfo PrivateSettingsDirectory { get; }
	
	/// <summary>
	/// Get a path to a writable file specific for this mod, within the directory that should be used to store private mod settings.
	/// This directory is usually stored in the user's application data folder and is not meant to be shared.
	/// </summary>
	/// <param name="fileExtension">The extension of the settings file (usually <c>json</c>).</param>
	/// <returns>The path to a writable file.</returns>
	IWritableFileInfo GetSinglePrivateSettingsFile(string fileExtension);

	/// <summary>
	/// Allows globally modifying all mod setting serializers.
	/// </summary>
	/// <param name="function">The function that will modify the serializer settings.</param>
	/// <param name="priority">The priority of the changes. Higher priority functions get called first.</param>
	void ApplyGlobalJsonSerializerSettings(Action<JsonSerializerSettings> function, double priority = 0);
	
	/// <summary>
	/// Allows modifying the mod setting serializer for this mod.
	/// </summary>
	/// <param name="function">The function that will modify the serializer settings.</param>
	/// <param name="priority">The priority of the changes. Higher priority functions get called first.</param>
	void ApplyJsonSerializerSettings(Action<JsonSerializerSettings> function, double priority = 0);
	
	/// <summary>The current serializer settings for this mod.</summary>
	JsonSerializerSettings JsonSerializerSettings { get; }
	
	/// <summary>The current serializer for this mod.</summary>
	JsonSerializer JsonSerializer { get; }

	/// <summary>
	/// Attemps to load and deserialize data from a JSON file.
	/// </summary>
	/// <typeparam name="T">The type to deserialize to.</typeparam>
	/// <param name="file">The file to load.</param>
	/// <param name="obj">The deserialized data, if succeeded.</param>
	/// <returns>Whether the file was loaded and deserialized correctly.</returns>
	bool TryLoadJson<T>(IFileInfo file, [MaybeNullWhen(false)] out T obj);
	
	/// <summary>
	/// Serializes data and saves it to a JSON file.
	/// </summary>
	/// <typeparam name="T">The type to serialize.</typeparam>
	/// <param name="file">The file to save to.</param>
	/// <param name="obj">The data to serialize.</param>
	void SaveJson<T>(IWritableFileInfo file, T obj);
}

/// <summary>
/// Hosts extensions for mod filesystem storage, relating to reference type-based data.
/// </summary>
public static class IModStorageClassExt
{
	/// <summary>
	/// Attemps to load and deserialize data from a JSON file.
	/// </summary>
	/// <typeparam name="T">The type to deserialize to.</typeparam>
	/// <param name="storage">The storage manager.</param>
	/// <param name="file">The file to load.</param>
	/// <returns>The deserialized data, or <c>null</c> if failed.</returns>
	public static T? LoadJsonOrNull<T>(this IModStorage storage, IWritableFileInfo file) where T : class
		=> storage.TryLoadJson(file, out T? settings) ? settings : null;

	/// <summary>
	/// Attemps to load and deserialize data from a JSON file, or creates a new file.
	/// </summary>
	/// <typeparam name="T">The type to deserialize to.</typeparam>
	/// <param name="storage">The storage manager.</param>
	/// <param name="file">The file to load.</param>
	/// <returns>The deserialized data, or a newly created instance of the data if failed.</returns>
	public static T LoadJson<T>(this IModStorage storage, IWritableFileInfo file) where T : class, new()
		=> storage.LoadJson(file, () => new T());

	/// <summary>
	/// Attemps to load and deserialize data from a JSON file, or creates a new file.
	/// </summary>
	/// <typeparam name="T">The type to deserialize to.</typeparam>
	/// <param name="storage">The storage manager.</param>
	/// <param name="file">The file to load.</param>
	/// <param name="default">The default data to use if the file could not be loaded or deserialized.</param>
	/// <returns>The deserialized data, or the provided default instance of data if failed.</returns>
	public static T LoadJson<T>(this IModStorage storage, IWritableFileInfo file, T @default) where T : class
		=> storage.LoadJson(file, () => @default);

	/// <summary>
	/// Attemps to load and deserialize data from a JSON file, or creates a new file.
	/// </summary>
	/// <typeparam name="T">The type to deserialize to.</typeparam>
	/// <param name="storage">The storage manager.</param>
	/// <param name="file">The file to load.</param>
	/// <param name="factory">A function that creates a new instance of data if the file could not be loaded or deserialized.</param>
	/// <returns>The deserialized data, or the newly created instance of data if failed.</returns>
	public static T LoadJson<T>(this IModStorage storage, IWritableFileInfo file, Func<T> factory) where T : class
	{
		if (storage.LoadJsonOrNull<T>(file) is { } settings)
			return settings;

		settings = factory();
		storage.SaveJson(file, settings);
		return settings;
	}
}

/// <summary>
/// Hosts extensions for mod filesystem storage, relating to value type-based data.
/// </summary>
public static class IModStorageStructExt
{
	/// <summary>
	/// Attemps to load and deserialize data from a JSON file.
	/// </summary>
	/// <typeparam name="T">The type to deserialize to.</typeparam>
	/// <param name="storage">The storage manager.</param>
	/// <param name="file">The file to load.</param>
	/// <returns>The deserialized data, or <c>null</c> if failed.</returns>
	public static T? LoadJsonOrNull<T>(this IModStorage storage, IWritableFileInfo file) where T : struct
		=> storage.TryLoadJson(file, out T? settings) ? settings : null;

	/// <summary>
	/// Attemps to load and deserialize data from a JSON file, or creates a new file.
	/// </summary>
	/// <typeparam name="T">The type to deserialize to.</typeparam>
	/// <param name="storage">The storage manager.</param>
	/// <param name="file">The file to load.</param>
	/// <returns>The deserialized data, or a newly created instance of the data if failed.</returns>
	public static T LoadJson<T>(this IModStorage storage, IWritableFileInfo file) where T : struct
		=> storage.LoadJson(file, () => new T());

	/// <summary>
	/// Attemps to load and deserialize data from a JSON file, or creates a new file.
	/// </summary>
	/// <typeparam name="T">The type to deserialize to.</typeparam>
	/// <param name="storage">The storage manager.</param>
	/// <param name="file">The file to load.</param>
	/// <param name="default">The default data to use if the file could not be loaded or deserialized.</param>
	/// <returns>The deserialized data, or the provided default instance of data if failed.</returns>
	public static T LoadJson<T>(this IModStorage storage, IWritableFileInfo file, T @default) where T : struct
		=> storage.LoadJson(file, () => @default);

	/// <summary>
	/// Attemps to load and deserialize data from a JSON file, or creates a new file.
	/// </summary>
	/// <typeparam name="T">The type to deserialize to.</typeparam>
	/// <param name="storage">The storage manager.</param>
	/// <param name="file">The file to load.</param>
	/// <param name="factory">A function that creates a new instance of data if the file could not be loaded or deserialized.</param>
	/// <returns>The deserialized data, or the newly created instance of data if failed.</returns>
	public static T LoadJson<T>(this IModStorage storage, IWritableFileInfo file, Func<T> factory) where T : struct
	{
		if (storage.LoadJsonOrNull<T>(file) is { } settings)
			return settings;

		settings = factory();
		storage.SaveJson(file, settings);
		return settings;
	}
}

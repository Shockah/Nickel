using Nanoray.PluginManager;
using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;

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

	bool TryLoadJson<T>(IFileInfo file, [MaybeNullWhen(false)] out T obj);
	void SaveJson<T>(IWritableFileInfo file, T obj);
}

public static class IModStorageClassExt
{
	public static T? LoadJsonOrNull<T>(this IModStorage storage, IWritableFileInfo file) where T : class
		=> storage.TryLoadJson(file, out T? settings) ? settings : null;

	public static T LoadJson<T>(this IModStorage storage, IWritableFileInfo file) where T : class, new()
		=> storage.LoadJson(file, () => new T());

	public static T LoadJson<T>(this IModStorage storage, IWritableFileInfo file, T @default) where T : class
		=> storage.LoadJson(file, () => @default);

	public static T LoadJson<T>(this IModStorage storage, IWritableFileInfo file, Func<T> factory) where T : class
	{
		if (storage.LoadJsonOrNull<T>(file) is { } settings)
			return settings;

		settings = factory();
		storage.SaveJson(file, settings);
		return settings;
	}
}

public static class IModStorageStructExt
{
	public static T? LoadJsonOrNull<T>(this IModStorage storage, IWritableFileInfo file) where T : struct
		=> storage.TryLoadJson(file, out T? settings) ? settings : null;

	public static T LoadJson<T>(this IModStorage storage, IWritableFileInfo file) where T : struct
		=> storage.LoadJson(file, () => new T());

	public static T LoadJson<T>(this IModStorage storage, IWritableFileInfo file, T @default) where T : struct
		=> storage.LoadJson(file, () => @default);

	public static T LoadJson<T>(this IModStorage storage, IWritableFileInfo file, Func<T> factory) where T : struct
	{
		if (storage.LoadJsonOrNull<T>(file) is { } settings)
			return settings;

		settings = factory();
		storage.SaveJson(file, settings);
		return settings;
	}
}

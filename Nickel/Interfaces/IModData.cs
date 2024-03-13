using System;
using System.Diagnostics.CodeAnalysis;

namespace Nickel;

/// <summary>
/// A mod-specific mod data manager.<br/>
/// Allows storing and retrieving arbitrary data on any objects. If the objects are persisted, this data will also be persisted.
/// </summary>
public interface IModData
{
	/// <summary>
	/// Retrieves previously stored data of a given type.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="o">The object to retrieve the data from.</param>
	/// <param name="key">The key to retrieve the data from.</param>
	/// <returns>The data.</returns>
	/// <exception cref="System.Collections.Generic.KeyNotFoundException">If there is no such data.</exception>
	T GetModData<T>(object o, string key);

	/// <summary>
	/// Tries to retrieve previously stored data of a given type.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="o">The object to retrieve the data from.</param>
	/// <param name="key">The key to retrieve the data from.</param>
	/// <param name="data">The data, if succeeded.</param>
	/// <returns>Whether the data was retrieved successfully.</returns>
	bool TryGetModData<T>(object o, string key, [MaybeNullWhen(false)] out T data);

	/// <summary>
	/// Retrieves previously stored data of a given type, or the given default value on no such data.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="o">The object to retrieve the data from.</param>
	/// <param name="key">The key to retrieve the data from.</param>
	/// <param name="defaultValue">The default value, if there is no data.</param>
	/// <returns>The data, or the given default value if there was no such data.</returns>
	T GetModDataOrDefault<T>(object o, string key, T defaultValue);

	/// <summary>
	/// Retrieves previously stored data of a given type, or the default value for that type on no such data.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="o">The object to retrieve the data from.</param>
	/// <param name="key">The key to retrieve the data from.</param>
	/// <returns>The data, or the default value if there was no such data.</returns>
	T GetModDataOrDefault<T>(object o, string key) where T : new();

	/// <summary>
	/// Retrieves previously stored data of a given type, or generates and stores a new value on no such data.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="o">The object to retrieve/store the data from/on.</param>
	/// <param name="key">The key to retrieve/store the data from/on.</param>
	/// <param name="factory">The new value factory.</param>
	/// <returns>The data.</returns>
	T ObtainModData<T>(object o, string key, Func<T> factory);

	/// <summary>
	/// Retrieves previously stored data of a given type, or stores the default value for that type on no such data.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="o">The object to retrieve/store the data from/on.</param>
	/// <param name="key">The key to retrieve/store the data from/on.</param>
	/// <returns>The data.</returns>
	T ObtainModData<T>(object o, string key) where T : new();

	/// <summary>
	/// Tests whether the given data is stored.
	/// </summary>
	/// <param name="o">The object to retrieve the data from.</param>
	/// <param name="key">The key to retrieve the data from.</param>
	/// <returns>Whether a value for the given key exists on the given object.</returns>
	bool ContainsModData(object o, string key);

	/// <summary>
	/// Stores a given value on the given object.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="o">The object to store the data on.</param>
	/// <param name="key">The key to store the data on.</param>
	/// <param name="data">The data to store.</param>
	void SetModData<T>(object o, string key, T data);

	/// <summary>
	/// Removes a given stored value from the given object.
	/// </summary>
	/// <param name="o">The object to remove the data from.</param>
	/// <param name="key">The key to remove the data from.</param>
	void RemoveModData(object o, string key);
}

/// <summary>
/// Hosts extensions for arbitrary mod data storage, relating to reference type-based data.
/// </summary>
public static class IModDataClassExt
{
	/// <summary>
	/// Retrieves previously stored data of a given type, or <c>null</c> on no such data.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="modData">The mod data manager.</param>
	/// <param name="o">The object to retrieve the data from.</param>
	/// <param name="key">The key to retrieve the data from.</param>
	/// <returns>The data, or <c>null</c> on no such data.</returns>
	public static T? GetOptionalModData<T>(this IModData modData, object o, string key) where T : class
		=> modData.TryGetModData<T>(o, key, out var data) ? data : null;

	/// <summary>
	/// Stores a given value on the given object, or removes it when given a <c>null</c> value.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="modData">The mod data manager.</param>
	/// <param name="o">The object to retrieve the data from.</param>
	/// <param name="key">The key to retrieve the data from.</param>
	/// <param name="data">The data to store.</param>
	public static void SetOptionalModData<T>(this IModData modData, object o, string key, T? data) where T : class
	{
		if (data != null)
			modData.SetModData(o, key, data);
		else
			modData.RemoveModData(o, key);
	}
}

/// <summary>
/// Hosts extensions for arbitrary mod data storage, relating to value type-based data.
/// </summary>
public static class IModDataStructExt
{
	/// <summary>
	/// Retrieves previously stored data of a given type, or <c>null</c> on no such data.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="modData">The mod data manager.</param>
	/// <param name="o">The object to retrieve the data from.</param>
	/// <param name="key">The key to retrieve the data from.</param>
	/// <returns>The data, or <c>null</c> on no such data.</returns>
	public static T? GetOptionalModData<T>(this IModData modData, object o, string key) where T : struct
		=> modData.TryGetModData<T>(o, key, out var data) ? data : null;

	/// <summary>
	/// Stores a given value on the given object, or removes it when given a <c>null</c> value.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="modData">The mod data manager.</param>
	/// <param name="o">The object to retrieve the data from.</param>
	/// <param name="key">The key to retrieve the data from.</param>
	/// <param name="data">The data to store.</param>
	public static void SetOptionalModData<T>(this IModData modData, object o, string key, T? data) where T : struct
	{
		if (data is { } nonNull)
			modData.SetModData(o, key, nonNull);
		else
			modData.RemoveModData(o, key);
	}
}

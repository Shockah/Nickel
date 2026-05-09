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
	T Get<T>(object o, string key);

	/// <summary>
	/// Tries to retrieve previously stored data of a given type.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="o">The object to retrieve the data from.</param>
	/// <param name="key">The key to retrieve the data from.</param>
	/// <param name="data">The data, if succeeded.</param>
	/// <returns>Whether the data was retrieved successfully.</returns>
	bool TryGet<T>(object o, string key, [MaybeNullWhen(false)] out T data);

	/// <summary>
	/// Retrieves previously stored data of a given type, or the given default value on no such data.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="o">The object to retrieve the data from.</param>
	/// <param name="key">The key to retrieve the data from.</param>
	/// <param name="defaultValue">The default value, if there is no data.</param>
	/// <returns>The data, or the given default value if there was no such data.</returns>
	T GetOrDefault<T>(object o, string key, T defaultValue);

	/// <summary>
	/// Retrieves previously stored data of a given type, or the default value for that type on no such data.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="o">The object to retrieve the data from.</param>
	/// <param name="key">The key to retrieve the data from.</param>
	/// <returns>The data, or the default value if there was no such data.</returns>
	T GetOrDefault<T>(object o, string key) where T : new();

	/// <summary>
	/// Retrieves previously stored data of a given type, or generates and stores a new value on no such data.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="o">The object to retrieve/store the data from/on.</param>
	/// <param name="key">The key to retrieve/store the data from/on.</param>
	/// <param name="factory">The new value factory.</param>
	/// <returns>The data.</returns>
	T Obtain<T>(object o, string key, Func<T> factory);

	/// <summary>
	/// Retrieves previously stored data of a given type, or stores the default value for that type on no such data.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="o">The object to retrieve/store the data from/on.</param>
	/// <param name="key">The key to retrieve/store the data from/on.</param>
	/// <returns>The data.</returns>
	T Obtain<T>(object o, string key) where T : new();

	/// <summary>
	/// Tests whether the given data is stored.
	/// </summary>
	/// <param name="o">The object to retrieve the data from.</param>
	/// <param name="key">The key to retrieve the data from.</param>
	/// <returns>Whether a value for the given key exists on the given object.</returns>
	bool Contains(object o, string key);

	/// <summary>
	/// Stores a given value on the given object.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="o">The object to store the data on.</param>
	/// <param name="key">The key to store the data on.</param>
	/// <param name="data">The data to store.</param>
	void Set<T>(object o, string key, T data);

	/// <summary>
	/// Removes a given stored value from the given object.
	/// </summary>
	/// <param name="o">The object to remove the data from.</param>
	/// <param name="key">The key to remove the data from.</param>
	void Remove(object o, string key);

	/// <summary>
	/// Copies all stored values owned by this mod from one object to another.
	/// </summary>
	/// <param name="from">The object to copy data from.</param>
	/// <param name="to">The object to copy data to.</param>
	void CopyOwned(object from, object to);

	/// <summary>
	/// Copies all stored values owned by any mod from one object to another.
	/// </summary>
	/// <param name="from">The object to copy data from.</param>
	/// <param name="to">The object to copy data to.</param>
	void CopyAll(object from, object to);
	
	/// <summary>
	/// Removes all stored values owned by this mod on the given object.
	/// </summary>
	/// <param name="o">The object to remove the data from.</param>
	void RemoveOwned(object o);

	/// <summary>
	/// Removes all stored values owned by any mod on the given object.
	/// </summary>
	/// <param name="o">The object to remove the data from.</param>
	void RemoveAll(object o);
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
	public static T? GetOptional<T>(this IModData modData, object o, string key) where T : class
		=> modData.TryGet<T>(o, key, out var data) ? data : null;

	/// <summary>
	/// Stores a given value on the given object, or removes it when given a <c>null</c> value.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="modData">The mod data manager.</param>
	/// <param name="o">The object to retrieve the data from.</param>
	/// <param name="key">The key to retrieve the data from.</param>
	/// <param name="data">The data to store.</param>
	public static void SetOptional<T>(this IModData modData, object o, string key, T? data) where T : class
	{
		if (data is null)
			modData.Remove(o, key);
		else
			modData.Set(o, key, data);
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
	public static T? GetOptional<T>(this IModData modData, object o, string key) where T : struct
		=> modData.TryGet<T>(o, key, out var data) ? data : null;

	/// <summary>
	/// Stores a given value on the given object, or removes it when given a <c>null</c> value.
	/// </summary>
	/// <typeparam name="T">The type of data.</typeparam>
	/// <param name="modData">The mod data manager.</param>
	/// <param name="o">The object to retrieve the data from.</param>
	/// <param name="key">The key to retrieve the data from.</param>
	/// <param name="data">The data to store.</param>
	public static void SetOptional<T>(this IModData modData, object o, string key, T? data) where T : struct
	{
		if (data is { } nonNull)
			modData.Set(o, key, nonNull);
		else
			modData.Remove(o, key);
	}
}

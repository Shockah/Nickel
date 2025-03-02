using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Nickel;

internal sealed class ConditionalWeakTableExtensionDataContractResolver(
	IContractResolver wrapped,
	ILogger logger,
	string jsonKey,
	ConditionalWeakTable<object, Dictionary<string, Dictionary<string, object?>>> storage
) : IContractResolver
{
	private readonly Dictionary<Type, JsonContract> ContractCache = new();

	public JsonContract ResolveContract(Type type)
	{
		if (this.ContractCache.TryGetValue(type, out var contract))
			return contract;

		contract = wrapped.ResolveContract(type);
		if (contract is JsonObjectContract objectContract)
		{
			var wrappedExtensionDataGetter = objectContract.ExtensionDataGetter;
			var wrappedExtensionDataSetter = objectContract.ExtensionDataSetter;
			objectContract.ExtensionDataGetter = o => this.ExtensionDataGetter(o, wrappedExtensionDataGetter);
			objectContract.ExtensionDataSetter = (o, key, value) => this.ExtensionDataSetter(o, key, value, wrappedExtensionDataSetter);
		}
		this.ContractCache[type] = contract;
		return contract;
	}

	private IEnumerable<KeyValuePair<object, object>> ExtensionDataGetter(object o, ExtensionDataGetter? wrappedAccessor)
	{
		if (storage.TryGetValue(o, out var allObjectData))
			yield return new(jsonKey, allObjectData);
		if (wrappedAccessor?.Invoke(o) is not { } wrappedData)
			yield break;
		foreach (var kvp in wrappedData)
			if (!Equals(kvp.Key, jsonKey))
				yield return kvp;
	}

	private void ExtensionDataSetter(object o, string key, object? value, ExtensionDataSetter? wrappedAccessor)
	{
		if (key != jsonKey)
		{
			wrappedAccessor?.Invoke(o, key, value);
			return;
		}
		if (value is null)
		{
			storage.Remove(o);
			return;
		}
		if (value is not Dictionary<string, Dictionary<string, object?>> dictionary)
		{
			logger.LogError("Encountered invalid serialized mod data of type {Type}.", value.GetType().FullName!);
			return;
		}
		storage.AddOrUpdate(o, dictionary);
	}
}

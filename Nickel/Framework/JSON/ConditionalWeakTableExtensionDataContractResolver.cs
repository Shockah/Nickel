using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Nickel;

internal sealed class ConditionalWeakTableExtensionDataContractResolver : IContractResolver
{
	private readonly IContractResolver Wrapped;
	private readonly ILogger Logger;
	private readonly string JsonKey;
	private readonly ConditionalWeakTable<object, Dictionary<string, Dictionary<string, object?>>> Storage;

	private readonly Dictionary<Type, JsonContract> ContractCache = new();

	public ConditionalWeakTableExtensionDataContractResolver(
		IContractResolver wrapped,
		ILogger logger,
		string jsonKey,
		ConditionalWeakTable<object, Dictionary<string, Dictionary<string, object?>>> storage
	)
	{
		this.Wrapped = wrapped;
		this.Logger = logger;
		this.JsonKey = jsonKey;
		this.Storage = storage;
	}

	public JsonContract ResolveContract(Type type)
	{
		if (this.ContractCache.TryGetValue(type, out var contract))
			return contract;

		contract = this.Wrapped.ResolveContract(type);
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

	private IEnumerable<KeyValuePair<object, object>> ExtensionDataGetter(object o, ExtensionDataGetter? wrapped)
	{
		if (this.Storage.TryGetValue(o, out var allObjectData))
			yield return new(this.JsonKey, allObjectData);
		if (wrapped?.Invoke(o) is { } wrappedData)
			foreach (var kvp in wrappedData)
				if (!Equals(kvp.Key, this.JsonKey))
					yield return kvp;
	}

	private void ExtensionDataSetter(object o, string key, object? value, ExtensionDataSetter? wrapped)
	{
		if (key != this.JsonKey)
		{
			wrapped?.Invoke(o, key, value);
			return;
		}
		if (value is null)
		{
			this.Storage.Remove(o);
			return;
		}
		if (value is not Dictionary<string, Dictionary<string, object?>> dictionary)
		{
			this.Logger.LogError("Encountered invalid serialized mod data of type {Type}.", value.GetType().FullName!);
			return;
		}
		this.Storage.AddOrUpdate(o, dictionary);
	}
}

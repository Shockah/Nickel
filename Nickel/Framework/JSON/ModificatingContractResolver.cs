using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace Nickel;

internal sealed class ModificatingContractResolver(
	Action<Type, JsonContract> contractModificator,
	IContractResolver? wrapped = null
) : IContractResolver
{
	private readonly IContractResolver Wrapped = wrapped ?? new DefaultContractResolver();

	private readonly Dictionary<Type, JsonContract> ContractCache = [];

	public JsonContract ResolveContract(Type type)
	{
		if (this.ContractCache.TryGetValue(type, out var contract))
			return contract;

		contract = this.Wrapped.ResolveContract(type);
		contractModificator(type, contract);

		this.ContractCache[type] = contract;
		return contract;
	}
}

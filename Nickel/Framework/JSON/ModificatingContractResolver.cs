using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace Nickel;

internal sealed class ModificatingContractResolver : IContractResolver
{
	private Action<Type, JsonContract> ContractModificator { get; }
	private IContractResolver Wrapped { get; }

	private Dictionary<Type, JsonContract> ContractCache { get; } = [];

	public ModificatingContractResolver(Action<Type, JsonContract> contractModificator, IContractResolver? wrapped = null)
	{
		this.ContractModificator = contractModificator;
		this.Wrapped = wrapped ?? new DefaultContractResolver();
	}

	public JsonContract ResolveContract(Type type)
	{
		if (this.ContractCache.TryGetValue(type, out var contract))
			return contract;

		contract = this.Wrapped.ResolveContract(type);
		this.ContractModificator(type, contract);

		this.ContractCache[type] = contract;
		return contract;
	}
}

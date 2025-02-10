using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;

namespace Nickel;

internal sealed class ModStorageManager(Func<IContractResolver?, IContractResolver>? contractResolverFactory = null)
{
	private readonly OrderedList<Action<JsonSerializerSettings>, double> GlobalSettingsFunctions = new(ascending: false);
	private readonly Dictionary<string, OrderedList<Action<JsonSerializerSettings>, double>> PerModSettingsFunctions = [];
	private readonly Dictionary<string, JsonSerializerSettings> CachedSerializerSettings = [];
	private readonly Dictionary<string, JsonSerializer> CachedSerializers = [];

	internal void ClearCache()
	{
		this.CachedSerializerSettings.Clear();
		this.CachedSerializers.Clear();
	}

	public void ApplyGlobalJsonSerializerSettings(Action<JsonSerializerSettings> function, double priority)
	{
		this.GlobalSettingsFunctions.Add(function, priority);
		this.ClearCache();
	}

	public void ApplyJsonSerializerSettingsForMod(IModManifest modManifest, Action<JsonSerializerSettings> function, double priority)
	{
		if (!this.PerModSettingsFunctions.TryGetValue(modManifest.UniqueName, out var perModFunctions))
		{
			perModFunctions = new(ascending: false);
			this.PerModSettingsFunctions[modManifest.UniqueName] = perModFunctions;
		}

		perModFunctions.Add(function, priority);
		this.CachedSerializerSettings.Remove(modManifest.UniqueName);
		this.CachedSerializers.Remove(modManifest.UniqueName);
	}

	public JsonSerializerSettings GetSerializerSettingsForMod(IModManifest modManifest)
	{
		if (!this.CachedSerializerSettings.TryGetValue(modManifest.UniqueName, out var settings))
		{
			settings = new JsonSerializerSettings { Formatting = Formatting.Indented };
			settings.Converters.Add(new StringEnumConverter());
			settings.Converters.Add(new SemanticVersionConverter());
			
			if (contractResolverFactory is not null)
				settings.ContractResolver = contractResolverFactory(settings.ContractResolver);

			var functions = new OrderedList<Action<JsonSerializerSettings>, double>(this.GlobalSettingsFunctions);

			if (this.PerModSettingsFunctions.TryGetValue(modManifest.UniqueName, out var perModFunctions))
				foreach (var (function, priority) in perModFunctions.Entries)
					functions.Add(function, priority);

			foreach (var function in functions)
				function(settings);

			this.CachedSerializerSettings[modManifest.UniqueName] = settings;
		}
		return settings;
	}

	public JsonSerializer GetSerializerForMod(IModManifest modManifest)
	{
		if (!this.CachedSerializers.TryGetValue(modManifest.UniqueName, out var serializer))
		{
			serializer = JsonSerializer.Create(this.GetSerializerSettingsForMod(modManifest));
			this.CachedSerializers[modManifest.UniqueName] = serializer;
		}
		return serializer;
	}
}

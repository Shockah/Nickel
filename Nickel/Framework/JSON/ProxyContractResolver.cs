using HarmonyLib;
using Nanoray.Pintail;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Nickel;

internal sealed class ProxyContractResolver<TContext> : JsonConverter
{
	private readonly IProxyManager<TContext> ProxyManager;
	private readonly Lazy<Dictionary<ProxyInfo<TContext>, IProxyFactory<TContext>>> Factories;
	private readonly Dictionary<Type, int> NestingCounter = [];

	public ProxyContractResolver(IProxyManager<TContext> proxyManager)
	{
		this.ProxyManager = proxyManager;
		this.Factories = new(() =>
		{
			var factoriesField = AccessTools.DeclaredField(typeof(ProxyManager<TContext>), "Factories")!;
			return (Dictionary<ProxyInfo<TContext>, IProxyFactory<TContext>>)factoriesField.GetValue(this.ProxyManager)!;
		});
	}

	public override bool CanConvert(Type objectType)
	{
		if (this.NestingCounter.GetValueOrDefault(objectType) != 0)
			return false;
		return objectType.IsAssignableTo(typeof(IProxyObject.IWithProxyTargetInstanceProperty)) || objectType.IsInterface;
	}

	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		try
		{
			this.NestingCounter[objectType] = this.NestingCounter.GetValueOrDefault(objectType) + 1;

			var token = JToken.Load(reader);
			if (token is not JObject @object || !@object.ContainsKey("ProxyInfo") || !@object.ContainsKey("Target"))
				return serializer.Deserialize(new JTokenReader(token), objectType);

			var serializableProxy = serializer.Deserialize<SerializableProxy>(new JTokenReader(@object));
			if (serializableProxy is null)
				return null;

			var proxyType = GetType(serializableProxy.ProxyInfo.Proxy) ?? throw new InvalidDataException($"Cannot deserialize {serializableProxy}");
			var obtainProxyMethod = typeof(IProxyManagerExtensions)
				.GetMethods()
				.First(m => m.Name == nameof(IProxyManagerExtensions.ObtainProxy))
				.MakeGenericMethod(typeof(TContext), proxyType);
			return obtainProxyMethod.Invoke(null, [this.ProxyManager, serializableProxy.Target, serializableProxy.ProxyInfo.Target.Context, serializableProxy.ProxyInfo.Proxy.Context]);

			Type? GetType(SerializableTypeInfo typeInfo)
			{
				try
				{
					var assemblyLoadContext = AssemblyLoadContext.All.FirstOrDefault(c => c.Name == typeInfo.AssemblyLoadContextName) ?? AssemblyLoadContext.Default;
					var assembly = assemblyLoadContext.Assemblies.FirstOrDefault(c => (c.GetName().Name ?? c.GetName().FullName) == typeInfo.AssemblyName) ?? assemblyLoadContext.LoadFromAssemblyName(new AssemblyName(typeInfo.AssemblyName));
					var type = assembly.GetTypes().FirstOrDefault(t => (t.FullName ?? t.Name) == typeInfo.TypeName);
					return type;
				}
				catch
				{
					return null;
				}
			}
		}
		finally
		{
			this.NestingCounter[objectType] = this.NestingCounter.GetValueOrDefault(objectType) - 1;
		}
	}

	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		if (value is null)
		{
			writer.WriteNull();
			return;
		}
		if (value is not IProxyObject.IWithProxyTargetInstanceProperty originalProxyObject)
			throw new ArgumentException($"Value {value} is not a proxy object", nameof(value));

		var finalProxyObject = originalProxyObject;
		var finalObject = value;

		while (finalObject is IProxyObject.IWithProxyTargetInstanceProperty nestedProxyObject)
		{
			finalProxyObject = nestedProxyObject;
			finalObject = nestedProxyObject.ProxyTargetInstance;
		}

		var originalProxyInfo = this.GetProxyInfoFromProxyObject(originalProxyObject);
		var finalProxyInfo = this.GetProxyInfoFromProxyObject(finalProxyObject);
		var serializableProxy = new SerializableProxy
		{
			ProxyInfo = (SerializableProxyInfo)new ProxyInfo<TContext>(finalProxyInfo.Target, originalProxyInfo.Proxy),
			Target = finalObject
		};
		serializer.Serialize(writer, serializableProxy);
	}

	private ProxyInfo<TContext> GetProxyInfoFromProxyObject(IProxyObject.IWithProxyTargetInstanceProperty proxy)
	{
		lock (this.Factories.Value)
		{
			foreach (var (proxyInfo, factory) in this.Factories.Value)
			{
				if (!factory.GetType().Name.StartsWith("InterfaceOrDelegateProxyFactory"))
					continue;
				var proxyCacheField = AccessTools.Field(factory.GetType(), "ProxyCache")!;
				var proxyCache = (ConditionalWeakTable<object, object>)proxyCacheField.GetValue(factory)!;

				lock (proxyCache)
				{
					if (proxyCache.TryGetValue(proxy.ProxyTargetInstance, out _))
						return proxyInfo;
				}
			}
		}
		throw new ArgumentException($"Could not retrieve ProxyInfo for proxy instance {proxy}", nameof(proxy));
	}

	private sealed class SerializableProxy
	{
		[JsonProperty]
		public required SerializableProxyInfo ProxyInfo { get; init; }

		[JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
		public required object Target { get; init; }
	}

	public sealed class SerializableProxyInfo
	{
		[JsonProperty]
		public required SerializableTypeInfo Target { get; init; }

		[JsonProperty]
		public required SerializableTypeInfo Proxy { get; init; }

		public static explicit operator SerializableProxyInfo(ProxyInfo<TContext> context)
			=> new()
			{
				Target = (SerializableTypeInfo)context.Target,
				Proxy = (SerializableTypeInfo)context.Proxy,
			};
	}

	public sealed class SerializableTypeInfo
	{
		[JsonProperty]
		public required string? AssemblyLoadContextName { get; init; }

		[JsonProperty]
		public required string AssemblyName { get; init; }

		[JsonProperty]
		public required string TypeName { get; init; }

		[JsonProperty]
		public required TContext Context { get; init; }

		public static explicit operator SerializableTypeInfo(TypeInfo<TContext> typeInfo)
			=> new()
			{
				AssemblyLoadContextName = AssemblyLoadContext.GetLoadContext(typeInfo.Type.Assembly)?.Name,
				AssemblyName = typeInfo.Type.Assembly.GetName().Name ?? typeInfo.Type.Assembly.GetName().FullName,
				TypeName = typeInfo.Type.FullName ?? typeInfo.Type.Name,
				Context = typeInfo.Context
			};
	}
}

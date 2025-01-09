using Nanoray.Pintail;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;

namespace Nickel;

internal sealed class ProxyContractResolver<TContext>(IProxyManager<TContext> proxyManager) : JsonConverter
{
	private readonly Dictionary<Type, int> NestingCounter = [];
	private readonly Dictionary<Type, Func<IProxyManager<TContext>, object, object>> ObtainProxyDelegates = [];
	private readonly Dictionary<SerializableTypeInfo, Type?> SerializableTypeToType = [];

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
			if (token is not JObject @object || !@object.ContainsKey(nameof(SerializableProxy.TypeInfo)) || !@object.ContainsKey(nameof(SerializableProxy.Target)))
				return serializer.Deserialize(new JTokenReader(token), objectType);

			var serializableProxy = serializer.Deserialize<SerializableProxy>(new JTokenReader(@object));
			if (serializableProxy is null)
				return null;

			var proxyType = GetTypeFromInfo(serializableProxy.TypeInfo) ?? throw new InvalidDataException($"Cannot deserialize {serializableProxy}");
			if (!this.ObtainProxyDelegates.TryGetValue(proxyType, out var obtainProxyDelegate))
			{
				var obtainProxyMethod = typeof(IProxyManagerExtensions)
					.GetMethods()
					.First(m => m.Name == nameof(IProxyManagerExtensions.ObtainProxy))
					.MakeGenericMethod(typeof(TContext), proxyType);
				
				var method = new DynamicMethod($"ObtainProxy_{proxyType.Name}", typeof(object), [typeof(IProxyManager<TContext>), typeof(object)]);
				var il = method.GetILGenerator();
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldstr, "!Serialization!");
				il.Emit(OpCodes.Ldstr, "!Serialization!");
				il.Emit(OpCodes.Call, obtainProxyMethod);
				il.Emit(OpCodes.Ret);
				
				obtainProxyDelegate = method.CreateDelegate<Func<IProxyManager<TContext>, object, object>>();
				this.ObtainProxyDelegates[proxyType] = obtainProxyDelegate;
			}
			return obtainProxyDelegate(proxyManager, serializableProxy.Target);

			Type? GetTypeFromInfo(SerializableTypeInfo typeInfo)
			{
				if (this.SerializableTypeToType.TryGetValue(typeInfo, out var type))
					return type;
				
				try
				{
					var assemblyLoadContext = AssemblyLoadContext.All.FirstOrDefault(c => c.Name == typeInfo.AssemblyLoadContextName) ?? AssemblyLoadContext.Default;
					var assembly = assemblyLoadContext.Assemblies.FirstOrDefault(c => (c.GetName().Name ?? c.GetName().FullName) == typeInfo.AssemblyName) ?? assemblyLoadContext.LoadFromAssemblyName(new AssemblyName(typeInfo.AssemblyName));
					type = assembly.GetType(typeInfo.TypeName);
					
					this.SerializableTypeToType[typeInfo] = type;
					return type;
				}
				catch
				{
					this.SerializableTypeToType[typeInfo] = null;
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
		if (GetChildMostTypes(originalProxyObject.GetType().GetInterfaces().Where(t => t.Assembly != typeof(IProxyManager<>).Assembly)).SingleOrDefault() is not { } proxyInterfaceType)
			throw new ArgumentException("Unknown proxy type to serialize", nameof(value));

		var finalObject = value;

		while (finalObject is IProxyObject.IWithProxyTargetInstanceProperty nestedProxyObject)
			finalObject = nestedProxyObject.ProxyTargetInstance;

		var serializableProxy = new SerializableProxy
		{
			TypeInfo = (SerializableTypeInfo)proxyInterfaceType,
			Target = finalObject
		};
		this.SerializableTypeToType[serializableProxy.TypeInfo] = proxyInterfaceType;
		serializer.Serialize(writer, serializableProxy);

		List<Type> GetChildMostTypes(IEnumerable<Type> types)
		{
			var results = new List<Type>();
			foreach (var type in types)
			{
				if (results.Any(result => result.IsAssignableTo(type)))
					continue;
				results.Add(type);
			}
			return results;
		}
	}

	private sealed class SerializableProxy
	{
		[JsonProperty]
		public required SerializableTypeInfo TypeInfo { get; init; }

		[JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
		public required object Target { get; init; }
	}

	public readonly struct SerializableTypeInfo
	{
		[JsonProperty]
		public required string? AssemblyLoadContextName { get; init; }

		[JsonProperty]
		public required string AssemblyName { get; init; }

		[JsonProperty]
		public required string TypeName { get; init; }

		public override bool Equals(object? obj)
			=> obj is SerializableTypeInfo typeInfo
			   && Equals(this.AssemblyLoadContextName, typeInfo.AssemblyLoadContextName)
			   && Equals(this.AssemblyName, typeInfo.AssemblyName)
			   && Equals(this.TypeName, typeInfo.TypeName);

		public override int GetHashCode()
			=> HashCode.Combine(this.AssemblyLoadContextName, this.AssemblyName, this.TypeName);

		public static explicit operator SerializableTypeInfo(Type type)
			=> new()
			{
				AssemblyLoadContextName = AssemblyLoadContext.GetLoadContext(type.Assembly)?.Name,
				AssemblyName = type.Assembly.GetName().Name ?? type.Assembly.GetName().FullName,
				TypeName = type.FullName ?? type.Name
			};
	}
}

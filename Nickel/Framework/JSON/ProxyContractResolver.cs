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

internal sealed class ProxyContractResolver(IProxyManager<string> proxyManager) : JsonConverter
{
	private readonly Dictionary<Type, int> NestingCounter = [];
	private readonly Dictionary<Type, Func<IProxyManager<string>, object, string, string, object>> ObtainProxyDelegates = [];
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
					.MakeGenericMethod(typeof(string), proxyType);
				
				var method = new DynamicMethod($"ObtainProxy_{proxyType.Name}", typeof(object), [typeof(IProxyManager<string>), typeof(object), typeof(string), typeof(string)]);
				var il = method.GetILGenerator();
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldarg_2);
				il.Emit(OpCodes.Ldarg_3);
				il.Emit(OpCodes.Call, obtainProxyMethod);
				il.Emit(OpCodes.Ret);
				
				obtainProxyDelegate = method.CreateDelegate<Func<IProxyManager<string>, object, string, string, object>>();
				this.ObtainProxyDelegates[proxyType] = obtainProxyDelegate;
			}
			return obtainProxyDelegate(proxyManager, serializableProxy.Target, serializableProxy.TargetContext ?? "!Serialization!", serializableProxy.ProxyContext ?? "!Serialization!");

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
		
		if (value is not IProxyObject)
			throw new ArgumentException($"Value {value} is not a proxy object", nameof(value));
		
		if (GetProxyType(value) is not { } proxyInterfaceType)
			throw new ArgumentException("Unknown proxy type to serialize", nameof(value));

		var serializableProxy = new SerializableProxy
		{
			TypeInfo = (SerializableTypeInfo)proxyInterfaceType,
			Target = GetRootTargetObject(value),
			ProxyContext = GetProxyContext(value),
			TargetContext = GetTargetContext(value),
		};
		this.SerializableTypeToType[serializableProxy.TypeInfo] = proxyInterfaceType;
		serializer.Serialize(writer, serializableProxy);

		static Type? GetProxyType(object @object)
			=> GetProxyTypeUsingMarkerInterface(@object) ?? GetProxyTypeUsingReflection(@object);

		static Type? GetProxyTypeUsingMarkerInterface(object @object)
			=> (@object as IProxyObject.IWithProxyInfoProperty<string>)?.ProxyInfo.Proxy.Type;

		static Type? GetProxyTypeUsingReflection(object @object)
		{
			var pintailAssembly = typeof(IProxyManager<>).Assembly;
			var results = new List<Type>();
			foreach (var interfaceType in @object.GetType().GetInterfaces())
			{
				if (interfaceType.Assembly == pintailAssembly)
					continue;
				if (results.Any(result => result.IsAssignableTo(interfaceType)))
					continue;
				results.Add(interfaceType);
			}
			return results.FirstOrDefault();
		}

		static string? GetProxyContext(object @object)
			=> (@object as IProxyObject.IWithProxyInfoProperty<string>)?.ProxyInfo.Proxy.Context;

		static string? GetTargetContext(object @object)
		{
			var finalObject = @object;
			while (finalObject is IProxyObject.IWithProxyTargetInstanceProperty nestedProxyObject)
				finalObject = nestedProxyObject.ProxyTargetInstance;
			return (finalObject as IProxyObject.IWithProxyInfoProperty<string>)?.ProxyInfo.Target.Context;
		}

		static object GetRootTargetObject(object @object)
		{
			var finalObject = @object;
			while (finalObject is IProxyObject.IWithProxyTargetInstanceProperty nestedProxyObject)
				finalObject = nestedProxyObject.ProxyTargetInstance;
			return finalObject;
		}
	}

	private sealed class SerializableProxy
	{
		[JsonProperty]
		public required SerializableTypeInfo TypeInfo { get; init; }

		[JsonProperty(TypeNameHandling = TypeNameHandling.Auto)]
		public required object Target { get; init; }
		
		public string? ProxyContext { get; init; }
		public string? TargetContext { get; init; }
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

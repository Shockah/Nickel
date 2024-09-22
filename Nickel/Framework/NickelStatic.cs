using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Nanoray.Mitosis;
using Nanoray.Pintail;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nickel;

internal static class NickelStatic
{
	private static readonly HashSet<Type> ErroredOutTypes = [];
	
	private static readonly Lazy<DefaultCloneEngine> CloneEngine = new(() =>
	{
		var pintailAssembly = typeof(IProxyManager<>).Assembly;
		
		var engine = new DefaultCloneEngine();
		engine.RegisterCloneListener(Nickel.Instance.ModManager.ModDataManager);
		engine.RegisterSpecializedEngine(new HashSetCloneEngine(engine));
		engine.RegisterFieldFilter(f =>
		{
			if (f.FieldType.Assembly == pintailAssembly)
				return DefaultCloneEngineFieldFilterBehavior.CopyValue;
			if (f.GetCustomAttribute<JsonIgnoreAttribute>() is not null)
				return DefaultCloneEngineFieldFilterBehavior.DoNotInitialize;
			if (f.FieldType.IsArray)
				return DefaultCloneEngineFieldFilterBehavior.Clone;
			if (f.IsPublic || f.GetCustomAttribute<JsonPropertyAttribute>() is not null)
				return DefaultCloneEngineFieldFilterBehavior.Clone;
			if (GetInterfacesRecursivelyAsEnumerable(f.DeclaringType!, includingSelf: true).Any(i => i.IsConstructedGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>)))
				return DefaultCloneEngineFieldFilterBehavior.Clone;
			return DefaultCloneEngineFieldFilterBehavior.CopyValue;
			
			static IEnumerable<Type> GetInterfacesRecursivelyAsEnumerable(Type type, bool includingSelf)
			{
				if (includingSelf && type.IsInterface)
					yield return type;
				foreach (var interfaceType in type.GetInterfaces())
				{
					yield return interfaceType;
					foreach (var recursiveInterfaceType in GetInterfacesRecursivelyAsEnumerable(interfaceType, false))
						yield return recursiveInterfaceType;
				}
			}
		});
		return engine;
	});
	
	private static readonly Lazy<JsonCloneEngine> JsonCloneEngine = new(() => new JsonCloneEngine(JSONSettings.serializer));

	[UsedImplicitly]
	public static T? DeepCopy<T>(T? original) where T : class
	{
		if (original is null)
			return null;
		
		try
		{
			return CloneEngine.Value.Clone(original);
		}
		catch (Exception ex)
		{
			if (ErroredOutTypes.Add(original.GetType()))
				Nickel.Instance.ModManager.Logger.LogError("Could not clone `{Value}` of type `{Type}` via Mitosis; falling back to JSON-based method: {Exception}", original, original.GetType(), ex);
			return JsonCloneEngine.Value.Clone(original);
		}
	}
}

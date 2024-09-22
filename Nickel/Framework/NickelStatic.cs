using Microsoft.Extensions.Logging;
using Nanoray.Mitosis;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nickel;

internal static class NickelStatic
{
	private static readonly HashSet<Type> ErroredOutTypes = [];
	
	private static readonly Lazy<DefaultCloneEngine> CloneEngine = new(() =>
	{
		var engine = new DefaultCloneEngine();
		engine.RegisterCloneListener(Nickel.Instance.ModManager.ModDataManager);
		engine.RegisterFieldFilter(f =>
		{
			var hasJsonProperty = f.GetCustomAttribute<JsonPropertyAttribute>() is not null;
			var hasJsonIgnore = f.GetCustomAttribute<JsonIgnoreAttribute>() is not null;
			if ((f.IsPublic || hasJsonProperty) && !hasJsonIgnore)
				return DefaultCloneEngineFieldFilterBehavior.Clone;

			if (f.DeclaringType!.IsAssignableTo(typeof(IReadOnlyList<object>)))
				return DefaultCloneEngineFieldFilterBehavior.Clone;
			if (f.DeclaringType!.IsAssignableTo(typeof(IReadOnlyDictionary<object, object>)))
				return DefaultCloneEngineFieldFilterBehavior.Clone;
			if (f.DeclaringType!.IsAssignableTo(typeof(IReadOnlySet<object>)))
				return DefaultCloneEngineFieldFilterBehavior.Clone;
			
			return DefaultCloneEngineFieldFilterBehavior.DoNotInitialize;
		});
		return engine;
	});
	
	private static readonly Lazy<JsonCloneEngine> JsonCloneEngine = new(() => new JsonCloneEngine(JSONSettings.serializer));

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

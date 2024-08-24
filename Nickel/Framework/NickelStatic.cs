using Microsoft.Extensions.Logging;
using Nanoray.Mitosis;
using System;
using System.Collections.Generic;

namespace Nickel;

internal static class NickelStatic
{
	private static readonly HashSet<Type> ErroredOutTypes = [];
	
	private static readonly Lazy<DefaultCloneEngine> CloneEngine = new(() =>
	{
		var engine = new DefaultCloneEngine();
		engine.RegisterCloneListener(Nickel.Instance.ModManager.ModDataManager);
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

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;

namespace Nickel;

internal sealed class GameTypeDictionaryFieldModDataHandler : IModDataHandler
{
	private readonly Func<ModLoadPhaseState> ModLoadPhaseStateProvider;
	private readonly Lazy<IModDataHandler> Handler;
	
	public GameTypeDictionaryFieldModDataHandler(string gameTypeName, Func<ModLoadPhaseState> modLoadPhaseStateProvider)
	{
		this.ModLoadPhaseStateProvider = modLoadPhaseStateProvider;
		this.Handler = new(() =>
		{
			var type = typeof(Card).Assembly.GetType(gameTypeName)!;
			var field = AccessTools.DeclaredField(type, ModDataFieldDefinitionEditor.FieldName);

			var fieldGetterDynamicMethod = new DynamicMethod($"get_{ModDataFieldDefinitionEditor.FieldName}", typeof(Dictionary<string, Dictionary<string, object?>>), [type]);
			{
				var il = fieldGetterDynamicMethod.GetILGenerator();
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldfld, field);
				il.Emit(OpCodes.Ret);
			}
			var fieldGetterDelegate = fieldGetterDynamicMethod.CreateDelegate(typeof(Func<,>).MakeGenericType(type, typeof(Dictionary<string, Dictionary<string, object?>>)));
				
			var fieldSetterDynamicMethod = new DynamicMethod($"set_{ModDataFieldDefinitionEditor.FieldName}", typeof(void), [type, typeof(Dictionary<string, Dictionary<string, object?>>)]);
			{
				var il = fieldSetterDynamicMethod.GetILGenerator();
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Stfld, field);
				il.Emit(OpCodes.Ret);
			}
			var fieldSetterDelegate = fieldSetterDynamicMethod.CreateDelegate(typeof(Action<,>).MakeGenericType(type, typeof(Dictionary<string, Dictionary<string, object?>>)));

			var handlerType = typeof(DictionaryModDataHandler<>).MakeGenericType(type);
			return (IModDataHandler)Activator.CreateInstance(handlerType, fieldGetterDelegate, fieldSetterDelegate)!;
		});
	}

	private IModDataHandler GetHandlerOrThrow()
	{
		if (this.ModLoadPhaseStateProvider().Phase < ModLoadPhase.AfterGameAssembly)
			throw new InvalidOperationException($"Cannot use `{nameof(GameTypeDictionaryFieldModDataHandler)}` before the game assembly is loaded");
		return this.Handler.Value;
	}
	
	public bool CanHandleType(Type type)
	{
		if (this.ModLoadPhaseStateProvider().Phase < ModLoadPhase.AfterGameAssembly)
			return false;
		return this.Handler.Value.CanHandleType(type);
	}

	public IModDataHandler GetUnderlyingHandler(object o)
		=> this.GetHandlerOrThrow();

	public T GetModData<T>(string modUniqueName, object o, string key)
		=> this.GetHandlerOrThrow().GetModData<T>(modUniqueName, o, key);

	public bool TryGetModData<T>(string modUniqueName, object o, string key, [MaybeNullWhen(false)] out T data)
		=> this.GetHandlerOrThrow().TryGetModData(modUniqueName, o, key, out data);

	public bool ContainsModData(string modUniqueName, object o, string key)
		=> this.GetHandlerOrThrow().ContainsModData(modUniqueName, o, key);

	public void SetModData<T>(string modUniqueName, object o, string key, T data)
		=> this.GetHandlerOrThrow().SetModData(modUniqueName, o, key, data);

	public void RemoveModData(string modUniqueName, object o, string key)
		=> this.GetHandlerOrThrow().RemoveModData(modUniqueName, o, key);

	public bool TryCopyOwnedModDataDirectly(string modUniqueName, object from, object to)
		=> this.GetHandlerOrThrow().TryCopyOwnedModDataDirectly(modUniqueName, from, to);

	public bool TryCopyAllModDataDirectly(object from, object to)
		=> this.GetHandlerOrThrow().TryCopyAllModDataDirectly(from, to);

	public bool TryRemoveOwnedModDataDirectly(string modUniqueName, object o)
		=> this.GetHandlerOrThrow().TryRemoveOwnedModDataDirectly(modUniqueName, o);

	public bool TryRemoveAllModDataDirectly(object o)
		=> this.GetHandlerOrThrow().TryRemoveAllModDataDirectly(o);

	public IEnumerable<KeyValuePair<string, object?>> GetAllOwnedModData(string modUniqueName, object o)
		=> this.GetHandlerOrThrow().GetAllOwnedModData(modUniqueName, o);

	public IEnumerable<KeyValuePair<string, IEnumerable<KeyValuePair<string, object?>>>> GetAllModData(object o)
		=> this.GetHandlerOrThrow().GetAllModData(o);

	public T ObtainModData<T>(string modUniqueName, object o, string key, Func<T> factory)
		=> this.GetHandlerOrThrow().ObtainModData(modUniqueName, o, key, factory);
}

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
		if (!this.ModLoadPhaseStateProvider().IsGameAssemblyLoaded)
			throw new InvalidOperationException($"Cannot use `{nameof(GameTypeDictionaryFieldModDataHandler)}` before the game assembly is loaded");
		return this.Handler.Value;
	}
	
	public bool CanHandleType(Type type)
	{
		if (!this.ModLoadPhaseStateProvider().IsGameAssemblyLoaded)
			return false;
		return this.Handler.Value.CanHandleType(type);
	}

	public T GetModData<T>(IModManifest manifest, object o, string key)
		=> this.GetHandlerOrThrow().GetModData<T>(manifest, o, key);

	public bool TryGetModData<T>(IModManifest manifest, object o, string key, [MaybeNullWhen(false)] out T data)
		=> this.GetHandlerOrThrow().TryGetModData(manifest, o, key, out data);

	public bool ContainsModData(IModManifest manifest, object o, string key)
		=> this.GetHandlerOrThrow().ContainsModData(manifest, o, key);

	public void SetModData<T>(IModManifest manifest, object o, string key, T data)
		=> this.GetHandlerOrThrow().SetModData(manifest, o, key, data);

	public void RemoveModData(IModManifest manifest, object o, string key)
		=> this.GetHandlerOrThrow().RemoveModData(manifest, o, key);

	public void CopyOwnedModData(IModManifest manifest, object from, object to)
		=> this.GetHandlerOrThrow().CopyOwnedModData(manifest, from, to);

	public void CopyAllModData(object from, object to)
		=> this.GetHandlerOrThrow().CopyAllModData(from, to);
	
	public T ObtainModData<T>(IModManifest manifest, object o, string key, Func<T> factory)
		=> this.GetHandlerOrThrow().ObtainModData(manifest, o, key, factory);
}

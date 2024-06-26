using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel;

/// <summary>
/// Hosts extensions for working with the reflection API.
/// </summary>
public static class ReflectionExt
{
	/// <summary>
	/// Emits a getter for a static property.
	/// </summary>
	/// <typeparam name="TValue">The type of the property.</typeparam>
	/// <param name="property">The property.</param>
	/// <returns>A getter function.</returns>
	public static Func<TValue> EmitStaticGetter<TValue>(this PropertyInfo property)
	{
		var method = new DynamicMethod($"get_{property.Name}", typeof(TValue), []);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Call, property.GetGetMethod(true)!);
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Func<TValue>>();
	}

	/// <summary>
	/// Emits a getter for a static field.
	/// </summary>
	/// <typeparam name="TValue">The type of the field.</typeparam>
	/// <param name="field">The field.</param>
	/// <returns>A getter function.</returns>
	public static Func<TValue> EmitStaticGetter<TValue>(this FieldInfo field)
	{
		var method = new DynamicMethod($"get_{field.Name}", typeof(TValue), []);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldsfld, field);
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Func<TValue>>();
	}

	/// <summary>
	/// Emits a setter for a static property.
	/// </summary>
	/// <typeparam name="TValue">The type of the property.</typeparam>
	/// <param name="property">The property.</param>
	/// <returns>A setter function.</returns>
	public static Action<TValue> EmitStaticSetter<TValue>(this PropertyInfo property)
	{
		var method = new DynamicMethod($"set_{property.Name}", typeof(void), [typeof(TValue)]);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Call, property.GetSetMethod(true)!);
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Action<TValue>>();
	}

	/// <summary>
	/// Emits a setter for a static field.
	/// </summary>
	/// <typeparam name="TValue">The type of the field.</typeparam>
	/// <param name="field">The field.</param>
	/// <returns>A setter function.</returns>
	public static Action<TValue> EmitStaticSetter<TValue>(this FieldInfo field)
	{
		var method = new DynamicMethod($"set_{field.Name}", typeof(void), [typeof(TValue)]);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Stsfld, field);
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Action<TValue>>();
	}

	/// <summary>
	/// Emits a getter for an instance property.
	/// </summary>
	/// <typeparam name="TOwner">The type that holds the property.</typeparam>
	/// <typeparam name="TValue">The type of the property.</typeparam>
	/// <param name="property">The property.</param>
	/// <returns>A getter function.</returns>
	public static Func<TOwner, TValue> EmitInstanceGetter<TOwner, TValue>(this PropertyInfo property)
	{
		var propertyAccessor = property.GetGetMethod(true)!;
		var method = new DynamicMethod($"get_{property.Name}", typeof(TValue), [typeof(TOwner)]);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(propertyAccessor is { IsVirtual: true, IsFinal: false } ? OpCodes.Callvirt : OpCodes.Call, propertyAccessor);
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Func<TOwner, TValue>>();
	}

	/// <summary>
	/// Emits a getter for an instance field.
	/// </summary>
	/// <typeparam name="TOwner">The type that holds the field.</typeparam>
	/// <typeparam name="TValue">The type of the field.</typeparam>
	/// <param name="field">The field.</param>
	/// <returns>A getter function.</returns>
	public static Func<TOwner, TValue> EmitInstanceGetter<TOwner, TValue>(this FieldInfo field)
	{
		var method = new DynamicMethod($"get_{field.Name}", typeof(TValue), [typeof(TOwner)]);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldfld, field);
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Func<TOwner, TValue>>();
	}

	/// <summary>
	/// Emits a setter for an instance property.
	/// </summary>
	/// <typeparam name="TOwner">The type that holds the property.</typeparam>
	/// <typeparam name="TValue">The type of the property.</typeparam>
	/// <param name="property">The property.</param>
	/// <returns>A setter function.</returns>
	public static Action<TOwner, TValue> EmitInstanceSetter<TOwner, TValue>(this PropertyInfo property)
	{
		var propertyAccessor = property.GetSetMethod(true)!;
		var method = new DynamicMethod($"set_{property.Name}", typeof(void), [typeof(TOwner), typeof(TValue)]);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldarg_1);
		il.Emit(propertyAccessor is { IsVirtual: true, IsFinal: false } ? OpCodes.Callvirt : OpCodes.Call, propertyAccessor);
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Action<TOwner, TValue>>();
	}

	/// <summary>
	/// Emits a setter for an instance field.
	/// </summary>
	/// <typeparam name="TOwner">The type that holds the field.</typeparam>
	/// <typeparam name="TValue">The type of the field.</typeparam>
	/// <param name="field">The field.</param>
	/// <returns>A setter function.</returns>
	public static Action<TOwner, TValue> EmitInstanceSetter<TOwner, TValue>(this FieldInfo field)
	{
		var method = new DynamicMethod($"set_{field.Name}", typeof(void), [typeof(TOwner), typeof(TValue)]);
		var il = method.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldarg_1);
		il.Emit(OpCodes.Stfld, field);
		il.Emit(OpCodes.Ret);
		return method.CreateDelegate<Action<TOwner, TValue>>();
	}
}

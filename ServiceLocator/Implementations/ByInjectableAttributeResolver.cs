using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Nanoray.ServiceLocator;

public sealed class ByInjectableAttributeResolver : IResolver
{
	private readonly Type Type;
	private readonly object? Instance;
	private readonly List<(Type Type, Delegate Delegate)> Delegates = [];

	public ByInjectableAttributeResolver(Type type)
	{
		this.Type = type;
		this.Instance = null;
		this.PrepareDelegates();
	}

	public ByInjectableAttributeResolver(object instance)
	{
		this.Type = instance.GetType();
		this.Instance = instance;
		this.PrepareDelegates();
	}

	private void PrepareDelegates()
	{
		var isStatic = this.Instance is null;

		foreach (var property in this.Type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | (isStatic ? BindingFlags.Static : BindingFlags.Instance)))
		{
			if (property.GetCustomAttribute<InjectableAttribute>() is null)
				continue;
			if (property.GetMethod is not { } getter)
				continue;

			var method = new DynamicMethod($"Resolve{property.Name}", property.PropertyType, isStatic ? [] : [typeof(object)]);
			var il = method.GetILGenerator();

			if (!isStatic)
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Castclass, this.Type);
			}
			il.Emit(getter.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getter);
			il.Emit(OpCodes.Ret);

			var @delegate = method.CreateDelegate(isStatic ? typeof(Func<>).MakeGenericType(property.PropertyType) : typeof(Func<,>).MakeGenericType(typeof(object), property.PropertyType));
			this.Delegates.Add((Type: property.PropertyType, Delegate: @delegate));
		}
		
		foreach (var field in this.Type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | (isStatic ? BindingFlags.Static : BindingFlags.Instance)))
		{
			if (field.GetCustomAttribute<InjectableAttribute>() is null)
				continue;

			var method = new DynamicMethod($"Resolve{field.Name}", field.FieldType, isStatic ? [] : [typeof(object)]);
			var il = method.GetILGenerator();
			
			if (!isStatic)
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Castclass, this.Type);
			}
			il.Emit(isStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field);
			il.Emit(OpCodes.Ret);

			var @delegate = method.CreateDelegate(isStatic ? typeof(Func<>).MakeGenericType(field.FieldType) : typeof(Func<,>).MakeGenericType(typeof(object), field.FieldType));
			this.Delegates.Add((Type: field.FieldType, Delegate: @delegate));
		}
		
		foreach (var typeMethod in this.Type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | (isStatic ? BindingFlags.Static : BindingFlags.Instance)))
		{
			if (typeMethod.GetCustomAttribute<InjectableAttribute>() is null)
				continue;
			if (typeMethod.ReturnType == typeof(void))
				continue;
			if (typeMethod.GetParameters().Length != 0)
				continue;

			var method = new DynamicMethod($"ResolveVia{typeMethod.Name}", typeMethod.ReturnType, isStatic ? [] : [typeof(object)]);
			var il = method.GetILGenerator();
			
			if (!isStatic)
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Castclass, this.Type);
			}
			il.Emit(typeMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, typeMethod);
			il.Emit(OpCodes.Ret);

			var @delegate = method.CreateDelegate(isStatic ? typeof(Func<>).MakeGenericType(typeMethod.ReturnType) : typeof(Func<,>).MakeGenericType(typeof(object), typeMethod.ReturnType));
			this.Delegates.Add((Type: typeMethod.ReturnType, Delegate: @delegate));
		}
	}

	/// <inheritdoc/>
	public bool CanResolve<TComponent>(IResolver rootResolver)
		=> this.Delegates.Any(e => e.Type.IsAssignableTo(typeof(TComponent)));

	/// <inheritdoc/>
	public bool TryResolve<TComponent>(IResolver rootResolver, [MaybeNullWhen(false)] out TComponent component)
	{
		foreach (var (type, rawDelegate) in this.Delegates)
		{
			if (!type.IsAssignableTo(typeof(TComponent)))
				continue;

			if (this.Instance is null)
			{
				var @delegate = (Func<TComponent>)rawDelegate;
				component = @delegate();
			}
			else
			{
				var @delegate = (Func<object, TComponent>)rawDelegate;
				component = @delegate(this.Instance!);
			}
			return true;
		}

		component = default;
		return false;
	}
}

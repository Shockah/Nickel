using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Nanoray.ServiceLocator;

public sealed class ByConstructorResolver : IResolver
{
	private static readonly Lazy<MethodInfo> CanResolveMethod = new(() => typeof(IResolver).GetMethods().First(m => m.Name == nameof(IResolver.CanResolve) && m.GetParameters().Length == 1));
	private static readonly Lazy<MethodInfo> TryResolveMethod = new(() => typeof(IResolver).GetMethods().First(m => m.Name == nameof(IResolver.TryResolve) && m.GetParameters().Length == 1));

	private readonly Dictionary<Type, Func<IResolver, bool>> CanResolveDelegates = [];
	private readonly Dictionary<Type, Delegate> Factories = [];
	private readonly HashSet<Type> BusyTypes = [];

	/// <inheritdoc/>
	public bool CanResolve<TComponent>(IResolver rootResolver)
	{
		try
		{
			if (!this.BusyTypes.Add(typeof(TComponent)))
				return false;
			return this.ObtainCanResolveDelegate<TComponent>()(rootResolver);
		}
		finally
		{
			this.BusyTypes.Remove(typeof(TComponent));
		}
	}

	/// <inheritdoc/>
	public bool TryResolve<TComponent>(IResolver rootResolver, [MaybeNullWhen(false)] out TComponent component)
	{
		try
		{
			if (!this.BusyTypes.Add(typeof(TComponent)))
			{
				component = default;
				return false;
			}
			return this.ObtainFactory<TComponent>()(rootResolver, out component);
		}
		finally
		{
			this.BusyTypes.Remove(typeof(TComponent));
		}
	}

	private Func<IResolver, bool> ObtainCanResolveDelegate<TComponent>()
	{
		if (!this.CanResolveDelegates.TryGetValue(typeof(TComponent), out var @delegate))
		{
			@delegate = CreateCanResolveDelegate<TComponent>();
			this.CanResolveDelegates[typeof(TComponent)] = @delegate;
		}
		return @delegate;
	}

	private TryResolveFactory<TComponent> ObtainFactory<TComponent>()
	{
		if (!this.Factories.TryGetValue(typeof(TComponent), out var factory))
		{
			factory = CreateFactory<TComponent>();
			this.Factories[typeof(TComponent)] = factory;
		}
		return (TryResolveFactory<TComponent>)factory;
	}
	
	private static Func<IResolver, bool> CreateCanResolveDelegate<TComponent>()
	{
		var factory = new DynamicMethod($"CanResolve{typeof(TComponent)}", typeof(bool), [typeof(IResolver)]);
		var il = factory.GetILGenerator();

		var ctors = typeof(TComponent).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
			.OrderByDescending(ctor => ctor.IsPublic)
			.ThenByDescending(ctor => ctor.GetParameters().Length);

		foreach (var ctor in ctors)
		{
			var parameters = ctor.GetParameters();
			if (parameters.Any(p => p.ParameterType.IsByRef))
				continue;
			
			var badCtorLabel = il.DefineLabel();

			foreach (var parameter in parameters)
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Callvirt, CanResolveMethod.Value.MakeGenericMethod(parameter.ParameterType));
				il.Emit(OpCodes.Brfalse, badCtorLabel);
			}

			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Ret);
			
			il.MarkLabel(badCtorLabel);
		}
		
		il.Emit(OpCodes.Ldc_I4_0);
		il.Emit(OpCodes.Ret);

		return factory.CreateDelegate<Func<IResolver, bool>>();
	}

	private static TryResolveFactory<TComponent> CreateFactory<TComponent>()
	{
		var factory = new DynamicMethod($"TryResolve{typeof(TComponent)}", typeof(bool), [typeof(IResolver), typeof(TComponent).MakeByRefType()]);
		var il = factory.GetILGenerator();

		var ctors = typeof(TComponent).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
			.OrderByDescending(ctor => ctor.IsPublic)
			.ThenByDescending(ctor => ctor.GetParameters().Length);

		foreach (var ctor in ctors)
		{
			var parameters = ctor.GetParameters();
			if (parameters.Any(p => p.ParameterType.IsByRef))
				continue;
			
			var badCtorLabel = il.DefineLabel();
			var componentLocals = parameters.Select(p => il.DeclareLocal(p.ParameterType)).ToList();

			for (var i = 0; i < parameters.Length; i++)
			{
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldloca, componentLocals[i]);
				il.Emit(OpCodes.Callvirt, TryResolveMethod.Value.MakeGenericMethod(parameters[i].ParameterType));
				il.Emit(OpCodes.Brfalse, badCtorLabel);
			}

			il.Emit(OpCodes.Ldarg_1);
			
			foreach (var componentLocal in componentLocals)
				il.Emit(OpCodes.Ldloc, componentLocal);
			
			il.Emit(OpCodes.Newobj, ctor);
			
			if (typeof(TComponent).IsValueType)
				il.Emit(OpCodes.Stobj, typeof(TComponent));
			else
				il.Emit(OpCodes.Stind_Ref);
			
			il.Emit(OpCodes.Ldc_I4_1);
			il.Emit(OpCodes.Ret);
			
			il.MarkLabel(badCtorLabel);
		}

		il.Emit(OpCodes.Ldarg_1);
		
		if (typeof(TComponent).IsValueType)
		{
			il.Emit(OpCodes.Initobj, typeof(TComponent));
		}
		else
		{
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Stind_Ref);
		}
		
		il.Emit(OpCodes.Ldc_I4_0);
		il.Emit(OpCodes.Ret);

		return factory.CreateDelegate<TryResolveFactory<TComponent>>();
	}

	private delegate bool TryResolveFactory<TComponent>(IResolver resolver, [MaybeNullWhen(false)] out TComponent component);
}

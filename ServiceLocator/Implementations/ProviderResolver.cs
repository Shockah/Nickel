using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Nanoray.ServiceLocator;

public sealed class ProviderResolver(
	IResolver resolver
) : IResolver
{
	private static readonly Lazy<MethodInfo> CanResolveMethod = new(() => typeof(IResolver).GetMethods().First(m => m.Name == nameof(IResolver.CanResolve) && m.GetParameters().Length == 1));
	private static readonly Lazy<MethodInfo> ResolveMethod = new(() => typeof(IResolver).GetMethods().First(m => m.Name == nameof(IResolver.Resolve) && m.GetParameters().Length == 1));
	
	private readonly Dictionary<Type, Func<IResolver, IResolver, bool>> CanResolveDelegates = [];
	private readonly Dictionary<Type, Delegate> ResolveDelegates = [];
	
	/// <inheritdoc/>
	public bool CanResolve<TComponent>(IResolver rootResolver)
	{
		if (resolver.CanResolve<TComponent>(rootResolver))
			return true;
		if (!typeof(TComponent).IsConstructedGenericType)
			return false;
		if (typeof(TComponent).GetGenericTypeDefinition() != typeof(Func<>))
			return false;
		
		var genericArgTypes = typeof(TComponent).GetGenericArguments();
		var realComponentType = genericArgTypes[0];

		if (!this.CanResolveDelegates.TryGetValue(realComponentType, out var canResolveDelegate))
		{
			var method = new DynamicMethod($"CanResolve{realComponentType.Name}", typeof(bool), [typeof(IResolver), typeof(IResolver)]);
			var il = method.GetILGenerator();
				
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Callvirt, CanResolveMethod.Value.MakeGenericMethod(realComponentType));
			il.Emit(OpCodes.Ret);
				
			canResolveDelegate = method.CreateDelegate<Func<IResolver, IResolver, bool>>();
			this.CanResolveDelegates[realComponentType] = canResolveDelegate;
		}
		return canResolveDelegate(resolver, rootResolver);
	}

	/// <inheritdoc/>
	public bool TryResolve<TComponent>(IResolver rootResolver, [MaybeNullWhen(false)] out TComponent component)
	{
		if (resolver.TryResolve(rootResolver, out component))
			return true;
		
		if (!this.CanResolve<TComponent>(rootResolver))
		{
			component = default;
			return false;
		}
		
		var genericArgTypes = typeof(TComponent).GetGenericArguments();
		var realComponentType = genericArgTypes[0];
		
		if (!this.ResolveDelegates.TryGetValue(realComponentType, out var resolveDelegate))
		{
			var createResolveDelegateMethod = this.GetType().GetMethod(nameof(CreateResolveDelegate), BindingFlags.NonPublic | BindingFlags.Instance)!.MakeGenericMethod(realComponentType);
			resolveDelegate = (Delegate)createResolveDelegateMethod.Invoke(this, BindingFlags.DoNotWrapExceptions, null, [rootResolver], null)!;
			this.ResolveDelegates[realComponentType] = resolveDelegate;
		}

		var typedDelegate = (Func<TComponent>)resolveDelegate;
		component = typedDelegate();
		return true;
	}

	[UsedImplicitly]
	private Func<TRealComponent> CreateResolveDelegate<TRealComponent>(IResolver rootResolver)
		=> () => CreateResolveDelegate<TRealComponent>()(resolver, rootResolver);

	private static Func<IResolver, IResolver, TRealComponent> CreateResolveDelegate<TRealComponent>()
	{
		var method = new DynamicMethod($"Resolve{typeof(TRealComponent).Name}", typeof(TRealComponent), [typeof(IResolver), typeof(IResolver)]);
		var il = method.GetILGenerator();
		
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldarg_1);
		il.Emit(OpCodes.Callvirt, ResolveMethod.Value.MakeGenericMethod(typeof(TRealComponent)));
		il.Emit(OpCodes.Ret);
		
		return method.CreateDelegate<Func<IResolver, IResolver, TRealComponent>>();
	}
}

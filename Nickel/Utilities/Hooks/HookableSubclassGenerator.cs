using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Nickel;

public sealed class HookableSubclassGenerator
{
	private AssemblyBuilder AssemblyBuilder { get; }
	private ModuleBuilder ModuleBuilder { get; }
	private ByParameterDelegateMapper ByParameterDelegateMapper { get; } = new();
	private ObjectifiedDelegateMapper ObjectifiedDelegateMapper { get; } = new();

	private int HookSubclassCounter { get; set; } = 0;
	private HashSet<Assembly> IgnoresAccessChecksToAssemblies { get; } = [];

	public HookableSubclassGenerator()
	{
		this.AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"{this.GetType().Namespace}.HookSubclasses, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
		this.ModuleBuilder = this.AssemblyBuilder.DefineDynamicModule($"{this.GetType().Namespace}.HookSubclasses");

		this.AddAccessCheckIgnoreAttribute(typeof(HookSubclassGlue).Assembly);
	}

	private void AddAccessCheckIgnoreAttribute(Assembly assembly)
	{
		if (this.IgnoresAccessChecksToAssemblies.Contains(assembly))
			return;

		this.IgnoresAccessChecksToAssemblies.Add(assembly);
		this.AssemblyBuilder.SetCustomAttribute(
			new CustomAttributeBuilder(
				typeof(IgnoresAccessChecksToAttribute).GetConstructor([typeof(string)])!,
				[assembly.GetName().Name!]
			)
		);
	}

	public GeneratedHookableSubclass<T> GenerateHookableSubclass<T>(Func<MethodInfo, Func<List<object?>, object?>?> resultReducerProvider)
		where T : class
	{
		if (!typeof(T).IsInterface && typeof(T).IsSealed)
			throw new ArgumentException($"Type {typeof(T)} has to be an interface or a non-sealed class", nameof(T));

		this.AddAccessCheckIgnoreAttribute(typeof(T).Assembly);

		var parentType = typeof(T).IsClass ? typeof(T) : typeof(object);
		var typeBuilder = this.ModuleBuilder.DefineType(
			name: $"HookSubclass{this.HookSubclassCounter++}_{typeof(T).Name}",
			attr: TypeAttributes.Public | TypeAttributes.Class,
			parent: parentType
		);

		if (typeof(T).IsInterface)
			typeBuilder.AddInterfaceImplementation(typeof(T));
		if (!typeof(T).IsAssignableTo(typeof(IHookable)))
			typeBuilder.AddInterfaceImplementation(typeof(IHookable));

		HookSubclassStaticGlue staticGlue = new(this.ByParameterDelegateMapper, this.ObjectifiedDelegateMapper);
		var callVoidHooksMethod = typeof(HookSubclassGlue).GetMethod(nameof(HookSubclassGlue.CallVoidHooks))!;

		var glueField = typeBuilder.DefineField(
			fieldName: "__Glue",
			type: typeof(HookSubclassGlue),
			attributes: FieldAttributes.Private | FieldAttributes.InitOnly
		);

		// generate constructor
		{
			var constructorBuilder = typeBuilder.DefineConstructor(
				attributes: MethodAttributes.Public,
				callingConvention: CallingConventions.Standard | CallingConventions.HasThis,
				parameterTypes: [typeof(HookSubclassGlue)]
			);

			var il = constructorBuilder.GetILGenerator();

			// call base constructor
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Call, parentType.GetConstructor([])!);

			// set target instance field
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Stfld, glueField);

			il.Emit(OpCodes.Ret);
		}

		// generate `IHookable.RegisterMethodHook`
		{
			var methodBuilder = typeBuilder.DefineMethod(
				name: nameof(IHookable.RegisterMethodHook),
				attributes: MethodAttributes.Public | MethodAttributes.Virtual
			);

			var tHookDelegateGenericType = methodBuilder.DefineGenericParameters(["THookDelegate"])[0];
			tHookDelegateGenericType.SetBaseTypeConstraint(typeof(Delegate));

			methodBuilder.SetReturnType(typeof(void));
			methodBuilder.SetParameters([typeof(MethodInfo), tHookDelegateGenericType, typeof(double)]);

			var il = methodBuilder.GetILGenerator();

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, glueField);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Ldarg_3);
			il.Emit(OpCodes.Call, typeof(HookSubclassGlue).GetMethods().First(m => m.Name == nameof(HookSubclassGlue.RegisterMethodHook)).MakeGenericMethod([tHookDelegateGenericType]));
			il.Emit(OpCodes.Ret);
		}

		// generate `IHookable.UnregisterMethodHook`
		{
			var methodBuilder = typeBuilder.DefineMethod(
				name: nameof(IHookable.UnregisterMethodHook),
				attributes: MethodAttributes.Public | MethodAttributes.Virtual
			);

			var tHookDelegateGenericType = methodBuilder.DefineGenericParameters(["THookDelegate"])[0];
			tHookDelegateGenericType.SetBaseTypeConstraint(typeof(Delegate));

			methodBuilder.SetReturnType(typeof(void));
			methodBuilder.SetParameters([typeof(MethodInfo), tHookDelegateGenericType]);

			var il = methodBuilder.GetILGenerator();

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, glueField);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Call, typeof(HookSubclassGlue).GetMethods().First(m => m.Name == nameof(HookSubclassGlue.UnregisterMethodHook)).MakeGenericMethod([tHookDelegateGenericType]));
			il.Emit(OpCodes.Ret);
		}

		var includePrivate = false;
		var methodsToProxy = typeof(T).IsInterface
			? typeof(T).FindInterfaceMethods(includePrivate).Distinct().ToList()
			: typeof(T).FindClassMethods(includePrivate).Distinct().ToList();

		// generate hookable methods
		foreach (var method in methodsToProxy)
		{
			if (method.DeclaringType == typeof(IHookable))
				continue;

			Func<List<object?>, object?>? resultReducer = null;
			if (method.ReturnType != typeof(void))
			{
				resultReducer = resultReducerProvider(method);
				if (resultReducer is null)
				{
					if (typeof(T).IsInterface)
						throw new ArgumentException($"`{nameof(resultReducerProvider)}` provided no `{nameof(resultReducer)}`, but it's required for method {method}");
					else
						continue;
				}
			}
			var hookedMethodIndex = staticGlue.RegisterHookedMethod(method, resultReducer);

			var methodBuilder = typeBuilder.DefineMethod(
				name: method.Name,
				attributes: MethodAttributes.Public | MethodAttributes.Virtual
			);
			var methodParameters = method.GetParameters();

			methodBuilder.SetSignature(
				returnType: method.ReturnType,
				returnTypeRequiredCustomModifiers: method.ReturnParameter.GetRequiredCustomModifiers(),
				returnTypeOptionalCustomModifiers: method.ReturnParameter.GetOptionalCustomModifiers(),
				parameterTypes: methodParameters.Select(p => p.ParameterType).ToArray(),
				parameterTypeRequiredCustomModifiers: methodParameters.Select(p => p.GetRequiredCustomModifiers()).ToArray(),
				parameterTypeOptionalCustomModifiers: methodParameters.Select(p => p.GetOptionalCustomModifiers()).ToArray()
			);

			var il = methodBuilder.GetILGenerator();
			var argumentArrayLocal = il.DeclareLocal(typeof(object?[]));
			var reducedResultLocal = resultReducer is null ? null : il.DeclareLocal(method.ReturnType);

			// create an array for all of the arguments
			il.Emit(OpCodes.Ldc_I4, methodParameters.Length);
			il.Emit(OpCodes.Newarr, typeof(object));
			il.Emit(OpCodes.Stloc, argumentArrayLocal);

			// put all arguments into the array
			for (var i = 0; i < methodParameters.Length; i++)
			{
				il.Emit(OpCodes.Ldloc, argumentArrayLocal);
				il.Emit(OpCodes.Ldc_I4, i);
				il.Emit(OpCodes.Ldarg, i + 1); // argument 0 is `this`

				if (methodParameters[i].ParameterType.IsByRef)
				{
					var nonRefType = methodParameters[i].ParameterType.GetElementType()!;
					if (!nonRefType.IsValueType)
					{
						il.Emit(OpCodes.Ldind_Ref);
					}
					else if (!nonRefType.IsPrimitive)
					{
						il.Emit(OpCodes.Ldobj, nonRefType);
						il.Emit(OpCodes.Box, nonRefType);
					}
					else if (nonRefType == typeof(bool) || nonRefType == typeof(byte))
					{
						il.Emit(OpCodes.Ldind_U1);
					}
					else if (nonRefType == typeof(sbyte))
					{
						il.Emit(OpCodes.Ldind_I1);
					}
					else if (nonRefType == typeof(char))
					{
						il.Emit(OpCodes.Ldind_U2);
						il.Emit(OpCodes.Box, typeof(char));
					}
					else if (nonRefType == typeof(ushort))
					{
						il.Emit(OpCodes.Ldind_U2);
					}
					else if (nonRefType == typeof(short))
					{
						il.Emit(OpCodes.Ldind_I2);
					}
					else if (nonRefType == typeof(uint))
					{
						il.Emit(OpCodes.Ldind_U4);
					}
					else if (nonRefType == typeof(int))
					{
						il.Emit(OpCodes.Ldind_I4);
					}
					else if (nonRefType == typeof(ulong))
					{
						il.Emit(OpCodes.Ldind_I8);
						il.Emit(OpCodes.Box, typeof(ulong));
					}
					else if (nonRefType == typeof(long))
					{
						il.Emit(OpCodes.Ldind_I8);
					}
					else if (nonRefType == typeof(float))
					{
						il.Emit(OpCodes.Ldind_R4);
					}
					else if (nonRefType == typeof(double))
					{
						il.Emit(OpCodes.Ldind_R8);
					}
					else
					{
						throw new ArgumentException($"Unsupported method parameter type {methodParameters[i]} in method {method}");
					}
				}
				else
				{
					if (methodParameters[i].ParameterType.IsValueType)
						il.Emit(OpCodes.Box, methodParameters[i].ParameterType);
				}

				il.Emit(OpCodes.Stelem_Ref);
			}

			// choose the hook method to call
			var callHooksMethod = method.ReturnType == typeof(void)
				? callVoidHooksMethod
				: typeof(HookSubclassGlue).GetMethods()
					.First(m => m.Name == nameof(HookSubclassGlue.CallResultHooks))
					.MakeGenericMethod([method.ReturnType]);

			// call hooks and reduce result
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, glueField);
			il.Emit(OpCodes.Ldc_I4, hookedMethodIndex);
			il.Emit(OpCodes.Ldloc, argumentArrayLocal);
			il.Emit(OpCodes.Call, callHooksMethod);
			if (reducedResultLocal is not null)
				il.Emit(OpCodes.Stloc, reducedResultLocal);

			// put back ref arguments
			for (var i = 0; i < methodParameters.Length; i++)
			{
				if (!methodParameters[i].ParameterType.IsByRef)
					continue;
				var nonRefType = methodParameters[i].ParameterType.GetElementType()!;

				il.Emit(OpCodes.Ldarg, i + 1); // argument 0 is `this`
				il.Emit(OpCodes.Ldloc, argumentArrayLocal);
				il.Emit(OpCodes.Ldc_I4, i);
				il.Emit(OpCodes.Ldelem_Ref);

				if (!nonRefType.IsValueType)
				{
					il.Emit(OpCodes.Stind_Ref);
				}
				else if (!nonRefType.IsPrimitive)
				{
					il.Emit(OpCodes.Unbox_Any, nonRefType);
					il.Emit(OpCodes.Stobj, nonRefType);
				}
				else if (nonRefType == typeof(bool) || nonRefType == typeof(byte))
				{
					il.Emit(OpCodes.Unbox_Any, nonRefType);
					il.Emit(OpCodes.Stind_I1);
				}
				else if (nonRefType == typeof(sbyte))
				{
					il.Emit(OpCodes.Stind_I1);
				}
				else if (nonRefType == typeof(char))
				{
					il.Emit(OpCodes.Unbox_Any, typeof(char));
					il.Emit(OpCodes.Stind_I2);
				}
				else if (nonRefType == typeof(ushort))
				{
					il.Emit(OpCodes.Unbox_Any, typeof(ushort));
					il.Emit(OpCodes.Stind_I1);
				}
				else if (nonRefType == typeof(short))
				{
					il.Emit(OpCodes.Stind_I2);
				}
				else if (nonRefType == typeof(uint))
				{
					il.Emit(OpCodes.Unbox_Any, typeof(uint));
					il.Emit(OpCodes.Stind_I1);
				}
				else if (nonRefType == typeof(int))
				{
					il.Emit(OpCodes.Stind_I4);
				}
				else if (nonRefType == typeof(ulong))
				{
					il.Emit(OpCodes.Unbox_Any, typeof(ulong));
					il.Emit(OpCodes.Stind_I8);
				}
				else if (nonRefType == typeof(long))
				{
					il.Emit(OpCodes.Stind_I8);
				}
				else if (nonRefType == typeof(float))
				{
					il.Emit(OpCodes.Stind_R4);
				}
				else if (nonRefType == typeof(double))
				{
					il.Emit(OpCodes.Stind_R8);
				}
				else
				{
					throw new ArgumentException($"Unsupported method parameter type {methodParameters[i]} in method {method}");
				}
			}

			// return reduced result
			if (reducedResultLocal is not null)
				il.Emit(OpCodes.Ldloc, reducedResultLocal);
			il.Emit(OpCodes.Ret);
		}

		var builtType = typeBuilder.CreateType()!;
		var actualConstructor = builtType.GetConstructor([typeof(HookSubclassGlue)])!;
		return new()
		{
			Type = builtType,
			Factory = () => (T)actualConstructor.Invoke([new HookSubclassGlue(staticGlue)])
		};
	}
}

public readonly struct GeneratedHookableSubclass<T>
{
	public Type Type { get; init; }
	public Func<T> Factory { get; init; }
}

file sealed class HookSubclassStaticGlue(
	ByParameterDelegateMapper byParameterDelegateMapper,
	ObjectifiedDelegateMapper objectifiedDelegateMapper
)
{
	public ByParameterDelegateMapper ByParameterDelegateMapper { get; } = byParameterDelegateMapper;
	public ObjectifiedDelegateMapper ObjectifiedDelegateMapper { get; } = objectifiedDelegateMapper;
	private List<(MethodInfo Method, Func<List<object?>, object?>? ResultReducer)> HookedMethods { get; } = [];

	public bool TryGetHookedMethod(int hookedMethodIndex, [MaybeNullWhen(false)] out (MethodInfo Method, Func<List<object?>, object?>? ResultReducer) result)
	{
		result = default;
		if (hookedMethodIndex < 0 || hookedMethodIndex >= this.HookedMethods.Count)
			return false;

		result = this.HookedMethods[hookedMethodIndex];
		return true;
	}

	public int RegisterHookedMethod(MethodInfo method, Func<List<object?>, object?>? resultReducer)
	{
		this.HookedMethods.Add((Method: method, ResultReducer: resultReducer));
		return this.HookedMethods.Count - 1;
	}
}

file sealed class HookSubclassGlue(
	HookSubclassStaticGlue staticGlue
)
{
	private HookSubclassStaticGlue StaticGlue { get; } = staticGlue;
	// TODO: probably move some of it to the StaticGlue for reusage
	private Dictionary<MethodInfo, Delegate> CompiledRegisterTypedMethodHookDelegates { get; } = [];
	private Dictionary<MethodInfo, OrderedList<Delegate, double>> CompiledDelegates { get; } = [];
	private Dictionary<MethodInfo, Dictionary<Delegate, Delegate>> OriginalToCompiledDelegates { get; } = [];

	private Action<HookSubclassGlue, MethodInfo, THookDelegate, double> CompileRegisterTypedMethodHook<THookDelegate>(MethodInfo method)
		where THookDelegate : Delegate
	{
		List<Type> delegateTypeParameterTypes = [];
		delegateTypeParameterTypes.AddRange(method.GetParameters().Select(p => p.ParameterType));
		delegateTypeParameterTypes.Add(method.ReturnType);
		var methodDelegateType = Expression.GetDelegateType(delegateTypeParameterTypes.ToArray());

		DynamicMethod dynamicMethod = new(
			name: $"Map_{typeof(THookDelegate)}_To_{method}",
			returnType: method.ReturnType,
			parameterTypes: [typeof(HookSubclassGlue), typeof(MethodInfo), typeof(THookDelegate), typeof(double)]
		);
		var il = dynamicMethod.GetILGenerator();
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Ldarg_1);
		il.Emit(OpCodes.Ldarg_2);
		il.Emit(OpCodes.Ldarg_3);
		il.Emit(OpCodes.Call, this.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).First(m => m.Name == nameof(this.RegisterTypedMethodHook)).MakeGenericMethod([methodDelegateType, typeof(THookDelegate)]));
		il.Emit(OpCodes.Ret);
		return dynamicMethod.CreateDelegate<Action<HookSubclassGlue, MethodInfo, THookDelegate, double>>();
	}

	private void RegisterTypedMethodHook<TMethodDelegate, THookDelegate>(MethodInfo method, THookDelegate hookDelegate, double priority)
		where TMethodDelegate : Delegate
		where THookDelegate : Delegate
	{
		var compiledDelegate = this.StaticGlue.ByParameterDelegateMapper.Map<THookDelegate, TMethodDelegate>(hookDelegate);
		var objectifiedDelegate = this.StaticGlue.ObjectifiedDelegateMapper.Map(compiledDelegate);

		if (!this.OriginalToCompiledDelegates.TryGetValue(method, out var originalToCompiledDelegates))
		{
			originalToCompiledDelegates = [];
			this.OriginalToCompiledDelegates[method] = originalToCompiledDelegates;
		}
		if (!this.CompiledDelegates.TryGetValue(method, out var compiledDelegates))
		{
			compiledDelegates = [];
			this.CompiledDelegates[method] = compiledDelegates;
		}

		compiledDelegates.Add(objectifiedDelegate, -priority);
	}

	public void RegisterMethodHook<THookDelegate>(MethodInfo method, THookDelegate hookDelegate, double priority)
		where THookDelegate : Delegate
	{
		if (!this.CompiledRegisterTypedMethodHookDelegates.TryGetValue(method, out var rawCompiledRegisterTypedMethodHookDelegate))
		{
			rawCompiledRegisterTypedMethodHookDelegate = this.CompileRegisterTypedMethodHook<THookDelegate>(method);
			this.CompiledRegisterTypedMethodHookDelegates[method] = rawCompiledRegisterTypedMethodHookDelegate;
		}

		var typedCompiledRegisterTypedMethodHookDelegate = (Action<HookSubclassGlue, MethodInfo, THookDelegate, double>)rawCompiledRegisterTypedMethodHookDelegate;
		typedCompiledRegisterTypedMethodHookDelegate(this, method, hookDelegate, priority);
	}

	public void UnregisterMethodHook<THookDelegate>(MethodInfo method, THookDelegate hookDelegate)
		where THookDelegate : Delegate
	{
		if (!this.OriginalToCompiledDelegates.TryGetValue(method, out var originalToCompiledDelegates))
			return;
		if (!this.CompiledDelegates.TryGetValue(method, out var compiledDelegates))
			return;
		if (!originalToCompiledDelegates.TryGetValue(hookDelegate, out var compiledDelegate))
			return;

		compiledDelegates.Remove(compiledDelegate);
		originalToCompiledDelegates.Remove(hookDelegate);
	}

	public void CallVoidHooks(int hookedMethodIndex, object?[] arguments)
	{
		if (!this.StaticGlue.TryGetHookedMethod(hookedMethodIndex, out var hookedMethod))
			throw new ArgumentException("Invalid hooked method", nameof(hookedMethodIndex));

		if (!this.CompiledDelegates.TryGetValue(hookedMethod.Method, out var compiledDelegates))
			return;
		foreach (var compiledDelegate in compiledDelegates)
		{
			var typedDelegate = (Action<object?[]>)compiledDelegate;
			typedDelegate(arguments);
		}
	}

	public T? CallResultHooks<T>(int hookedMethodIndex, object?[] arguments)
	{
		if (!this.StaticGlue.TryGetHookedMethod(hookedMethodIndex, out var hookedMethod))
			throw new ArgumentException("Invalid hooked method", nameof(hookedMethodIndex));
		if (hookedMethod.ResultReducer is not { } resultReducer)
			throw new ArgumentException("Invalid hooked method", nameof(hookedMethodIndex));

		if (!this.CompiledDelegates.TryGetValue(hookedMethod.Method, out var compiledDelegates))
			return default;

		List<object?> delegateResults = new(capacity: compiledDelegates.Count);
		foreach (var compiledDelegate in compiledDelegates)
		{
			var typedDelegate = (Func<object?[], object?>)compiledDelegate;
			delegateResults.Add(typedDelegate(arguments));
		}
		return (T?)resultReducer(delegateResults);
	}
}

file sealed class DelegateHolder<TDelegate>(
	TDelegate @delegate
)
	where TDelegate : Delegate
{
	public TDelegate Delegate { get; } = @delegate;
}

file static class TypeExtensions
{
	public static IEnumerable<MethodInfo> FindClassMethods(this Type baseType, bool includePrivate, Func<MethodInfo, bool>? filter = null)
	{
		filter ??= (_) => true;
		return baseType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy | (includePrivate ? BindingFlags.NonPublic : 0))
			.Where(m => m.IsVirtual && !m.IsFinal && filter(m));
	}

	public static IEnumerable<MethodInfo> FindInterfaceMethods(this Type baseType, bool includePrivate, Func<MethodInfo, bool>? filter = null)
	{
		filter ??= (_) => true;
		foreach (var method in baseType.GetMethods(BindingFlags.Instance | BindingFlags.Public | (includePrivate ? BindingFlags.NonPublic : 0)))
			if (filter(method))
				yield return method;
		foreach (var interfaceType in baseType.GetInterfaces())
			foreach (var method in FindInterfaceMethods(interfaceType, includePrivate, filter))
				if (filter(method))
					yield return method;
	}
}

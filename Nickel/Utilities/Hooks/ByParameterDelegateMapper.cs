using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Nickel;

public sealed class ByParameterDelegateMapper
{
	private ModuleBuilder ModuleBuilder { get; }
	private Dictionary<Type, Dictionary<Type, Func<Delegate, Delegate>>> Cache { get; } = [];
	private int Counter { get; set; } = 0;

	public ByParameterDelegateMapper()
	{
		var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"{this.GetType().Namespace}.ByParameterDelegateMappers, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
		this.ModuleBuilder = assemblyBuilder.DefineDynamicModule($"{this.GetType().Namespace}.ByParameterDelegateMappers");
	}

	public TLongDelegate Map<TShortDelegate, TLongDelegate>(TShortDelegate @delegate, ParameterInfo[] longDelegateParameters)
		where TShortDelegate : Delegate
		where TLongDelegate : Delegate
	{
		if (this.Cache.TryGetValue(typeof(TLongDelegate), out var longDelegateTypeCache) && longDelegateTypeCache.TryGetValue(typeof(TShortDelegate), out var compiledFactory))
			return (TLongDelegate)compiledFactory(@delegate);

		var shortDelegateInvokeMethod = @delegate.GetMethodInfo();
		var longDelegateInvokeMethod = typeof(TLongDelegate).GetMethod("Invoke")!;

		if (shortDelegateInvokeMethod.ReturnType != longDelegateInvokeMethod.ReturnType)
			throw new ArgumentException($"Delegate `{typeof(TShortDelegate)}` has a return type `{shortDelegateInvokeMethod.ReturnType}` that does not match the return type `{longDelegateInvokeMethod.ReturnType}` for delegate `{typeof(TLongDelegate)}`", nameof(@delegate));

		var shortDelegateParameters = shortDelegateInvokeMethod.GetParameters();
		var parameterMapping = new int[shortDelegateParameters.Length];

		if (shortDelegateParameters.Length > parameterMapping.Length)
			throw new ArgumentException($"Delegate `{typeof(TShortDelegate)}` is not a valid hook for delegate `{typeof(TLongDelegate)}`", nameof(@delegate));

		for (var i = 0; i < shortDelegateParameters.Length; i++)
		{
			var shortDelegateParameter = shortDelegateParameters[i];
			var expectedName = shortDelegateParameter.GetCustomAttribute<MappedParameterNameAttribute>()?.Name ?? shortDelegateParameter.Name;
			var longDelegateParameterIndex = -1;

			for (var j = 0; j < longDelegateParameters.Length; j++)
			{
				if (longDelegateParameters[i].Name != expectedName)
					continue;
				if (!shortDelegateParameter.ParameterType.IsAssignableFrom(longDelegateParameters[i].ParameterType))
					throw new ArgumentException($"Delegate `{@delegate}` specifies a method parameter named `{expectedName}` which does exist on delegate `{typeof(TLongDelegate)}`, but its type `{longDelegateParameters[i].ParameterType}` is not compatible with the delegate parameter type `{shortDelegateParameter.ParameterType}`", nameof(@delegate));

				longDelegateParameterIndex = j;
				break;
			}

			if (longDelegateParameterIndex == -1)
				throw new ArgumentException($"Delegate `{@delegate}` specifies a method parameter named `{expectedName}` which does not exist on delegate `{typeof(TLongDelegate)}`", nameof(@delegate));
			parameterMapping[i] = longDelegateParameterIndex;
		}

		// start generating type
		var typeBuilder = this.ModuleBuilder.DefineType(
			name: $"MappedByParameterDelegate{this.Counter++}_{typeof(TShortDelegate)}_{typeof(TLongDelegate)}",
			attr: TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class
		);

		var delegateField = typeBuilder.DefineField(
			fieldName: "__Delegate",
			type: typeof(TShortDelegate),
			attributes: FieldAttributes.Private | FieldAttributes.InitOnly
		);

		// generate constructor
		{
			var constructorBuilder = typeBuilder.DefineConstructor(
				attributes: MethodAttributes.Public,
				callingConvention: CallingConventions.Standard | CallingConventions.HasThis,
				parameterTypes: [typeof(TShortDelegate)]
			);

			var il = constructorBuilder.GetILGenerator();

			// call base constructor
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Call, typeof(object).GetConstructor([])!);

			// set delegate field
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Stfld, delegateField);

			il.Emit(OpCodes.Ret);
		}

		// generate invoke method
		{
			var methodBuilder = typeBuilder.DefineMethod(
				name: "Invoke",
				attributes: MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual,
				returnType: shortDelegateInvokeMethod.ReturnType,
				parameterTypes: longDelegateParameters.Select(p => p.ParameterType).ToArray()
			);

			var il = methodBuilder.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, delegateField);
			for (var i = 0; i < parameterMapping.Length; i++)
				il.Emit(OpCodes.Ldarg, parameterMapping[i] + 1);
			il.Emit(OpCodes.Call, shortDelegateInvokeMethod);
			il.Emit(OpCodes.Ret);
		}

		(this.ModuleBuilder.Assembly as AssemblyBuilder)?.SetCustomAttribute(new CustomAttributeBuilder
		(
			typeof(IgnoresAccessChecksToAttribute).GetConstructor(new Type[] { typeof(string) })!,
			new object[] { shortDelegateInvokeMethod.DeclaringType!.Assembly.GetName().Name! }
		));

		var builtType = typeBuilder.CreateType()!;
		var actualConstructor = builtType.GetConstructor([typeof(TShortDelegate)])!;
		var actualInvokeMethod = builtType.GetMethod("Invoke")!;
		compiledFactory = shortDelegate =>
		{
			var @object = actualConstructor.Invoke([shortDelegate]);
			return Delegate.CreateDelegate(typeof(TLongDelegate), @object, actualInvokeMethod);
		};

		if (!this.Cache.TryGetValue(typeof(TLongDelegate), out longDelegateTypeCache))
		{
			longDelegateTypeCache = [];
			this.Cache[typeof(TLongDelegate)] = longDelegateTypeCache;
		}
		longDelegateTypeCache[typeof(TShortDelegate)] = compiledFactory;
		return (TLongDelegate)compiledFactory(@delegate);
	}
}

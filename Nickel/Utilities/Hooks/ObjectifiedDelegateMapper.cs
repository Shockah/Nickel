using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Nickel;

public sealed class ObjectifiedDelegateMapper
{
	private ModuleBuilder ModuleBuilder { get; }
	private Dictionary<Type, Func<Delegate, Delegate>> Cache { get; } = [];
	private int Counter { get; set; } = 0;

	public ObjectifiedDelegateMapper()
	{
		var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName($"{this.GetType().Namespace}.ObjectifiedDelegateMappers, Version={this.GetType().Assembly.GetName().Version}, Culture=neutral"), AssemblyBuilderAccess.Run);
		this.ModuleBuilder = assemblyBuilder.DefineDynamicModule($"{this.GetType().Namespace}.ObjectifiedDelegateMappers");
	}

	public Delegate Map<TDelegate>(TDelegate @delegate)
		where TDelegate : Delegate
	{
		if (this.Cache.TryGetValue(typeof(TDelegate), out var compiledFactory))
			return compiledFactory(@delegate);

		var normalDelegateInvokeMethod = typeof(TDelegate).GetMethod("Invoke")!;
		var objectDelegateType = normalDelegateInvokeMethod.ReturnType == typeof(void) ? typeof(Action<object?[]>) : typeof(Func<object?[], object?>);
		var objectDelegateInvokeMethod = objectDelegateType.GetMethod("Invoke")!;
		var normalDelegateParameters = normalDelegateInvokeMethod.GetParameters();

		// start generating type
		var typeBuilder = this.ModuleBuilder.DefineType(
			name: $"ObjectifiedDelegate{this.Counter++}_{typeof(TDelegate)}",
			attr: TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class
		);

		var delegateField = typeBuilder.DefineField(
			fieldName: "__Delegate",
			type: typeof(TDelegate),
			attributes: FieldAttributes.Private | FieldAttributes.InitOnly
		);

		// generate constructor
		{
			var constructorBuilder = typeBuilder.DefineConstructor(
				attributes: MethodAttributes.Public,
				callingConvention: CallingConventions.Standard | CallingConventions.HasThis,
				parameterTypes: [typeof(TDelegate)]
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
				returnType: objectDelegateInvokeMethod.ReturnType,
				parameterTypes: [typeof(object?[])]
			);

			var il = methodBuilder.GetILGenerator();
			var argumentLocals = normalDelegateParameters.Select(p => il.DeclareLocal(p.ParameterType)).ToArray();
			var resultLocal = objectDelegateInvokeMethod.ReturnType == typeof(void) ? null : il.DeclareLocal(typeof(object));

			// take out arguments from the array
			for (var i = 0; i < argumentLocals.Length; i++)
			{
				var argumentType = normalDelegateParameters[i].ParameterType;

				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldc_I4, i);
				il.Emit(OpCodes.Ldelem_Ref);
				il.Emit(argumentType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, argumentType);
				il.Emit(OpCodes.Stloc, argumentLocals[i]);
			}

			// call delegate
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, delegateField);

			for (var i = 0; i < argumentLocals.Length; i++)
			{
				var argumentType = normalDelegateParameters[i].ParameterType;
				il.Emit(argumentType.IsByRef ? OpCodes.Ldloca : OpCodes.Ldloc, argumentLocals[i]);
			}

			il.Emit(OpCodes.Call, normalDelegateInvokeMethod);
			if (resultLocal is not null)
			{
				if (normalDelegateInvokeMethod.ReturnType.IsValueType)
					il.Emit(OpCodes.Box, normalDelegateInvokeMethod.ReturnType);
				il.Emit(OpCodes.Stloc, resultLocal);
			}

			// put back ref arguments into the array
			for (var i = 0; i < argumentLocals.Length; i++)
			{
				var argumentType = normalDelegateParameters[i].ParameterType;
				if (!argumentType.IsByRef)
					continue;

				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldc_I4, i);
				il.Emit(OpCodes.Ldloc, argumentLocals[i]);
				if (normalDelegateInvokeMethod.ReturnType.IsValueType)
					il.Emit(OpCodes.Box, argumentType);
				il.Emit(OpCodes.Stelem_Ref);
			}

			if (resultLocal is not null)
				il.Emit(OpCodes.Ldloc, resultLocal);
			il.Emit(OpCodes.Ret);
		}

		var builtType = typeBuilder.CreateType()!;
		var actualConstructor = builtType.GetConstructor([typeof(TDelegate)])!;
		var actualInvokeMethod = builtType.GetMethod("Invoke")!;
		compiledFactory = @delegate =>
		{
			var @object = actualConstructor.Invoke([@delegate]);
			return Delegate.CreateDelegate(objectDelegateType, @object, actualInvokeMethod);
		};

		this.Cache[typeof(TDelegate)] = compiledFactory;
		return compiledFactory(@delegate);
	}
}

// ReSharper disable InconsistentNaming
#pragma warning disable CS0618 // Type or member is obsolete
using Mono.Cecil;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;
using System;
using System.Linq;
using System.Reflection.Emit;
using SOpCodes = System.Reflection.Emit.OpCodes;
using COpCodes = Mono.Cecil.Cil.OpCodes;

namespace Nickel;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public static partial class RewriteFacades
{
	public static partial class V1_2
	{
		private static readonly Lazy<Func<MapNodeContents, State, Route>> Emitted_MapNodeContents_MakeRoute = new(() =>
		{
			var mapNodeContentsType = typeof(MapNodeContents);
			var makeRouteMethod = mapNodeContentsType.GetMethod("MakeRoute")!;
			
			var method = new DynamicMethod($"{nameof(RewriteFacades)}_{nameof(V1_2)}_{nameof(Emitted_MapNodeContents_MakeRoute)}", typeof(Route), [typeof(MapNodeContents), typeof(State)]);
			var il = method.GetILGenerator();

			il.Emit(SOpCodes.Ldarg_0);
			il.Emit(SOpCodes.Ldarg_1);
			il.Emit(SOpCodes.Callvirt, makeRouteMethod);
			il.Emit(SOpCodes.Ret);

			return method.CreateDelegate<Func<MapNodeContents, State, Route>>();
		});
		
		public static Route MapNodeContents_MakeRoute(MapNodeContents @this, State s)
			=> Emitted_MapNodeContents_MakeRoute.Value(@this, s);
	}
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

internal sealed class V1_2_MapNodeContents_MakeRoute_DefinitionEditor : IAssemblyDefinitionEditor
{
	public bool WillEditAssembly(string fileBaseName)
		=> fileBaseName != "CobaltCore.dll";
	
	public bool EditAssemblyDefinition(AssemblyDefinition definition, Action<AssemblyEditorResult.Message> logger)
	{
		var didAnything = false;
		foreach (var module in definition.Modules)
			didAnything |= this.HandleModule(module, logger);
		return didAnything;
	}

	private bool HandleModule(ModuleDefinition module, Action<AssemblyEditorResult.Message> logger)
	{
		if (module.AssemblyReferences.FirstOrDefault(r => r.Name == "CobaltCore") is not { } cobaltCoreAssemblyNameReference)
			return false;
		var cobaltCoreAssemblyDefinition = module.AssemblyResolver.Resolve(cobaltCoreAssemblyNameReference);
		
		if (cobaltCoreAssemblyDefinition.MainModule.GetType(nameof(Route)) is not { } routeTypeReference)
			return false;
		if (cobaltCoreAssemblyDefinition.MainModule.GetType(nameof(State)) is not { } stateTypeReference)
			return false;
		if (cobaltCoreAssemblyDefinition.MainModule.GetType(nameof(Vec)) is not { } vecTypeReference)
			return false;
		
		// if any of these fail, we're not on 1.2
		if (cobaltCoreAssemblyDefinition.MainModule.GetType(nameof(MapNodeContents)) is not { } mapNodeContentsTypeReference)
			return false;
		if (mapNodeContentsTypeReference.Methods.FirstOrDefault(m => m.Name == nameof(MapNodeContents.MakeRoute)) is not { } makeRouteMethodReference)
			return false;
		if (makeRouteMethodReference.Parameters.Count != 2)
			return false;
		if (makeRouteMethodReference.ReturnType.FullName != routeTypeReference.FullName)
			return false;
		if (makeRouteMethodReference.Parameters[0].ParameterType.FullName != stateTypeReference.FullName)
			return false;
		if (makeRouteMethodReference.Parameters[1].ParameterType.FullName != vecTypeReference.FullName)
			return false;
		
		var didAnything = false;
		foreach (var type in module.Types)
			didAnything |= this.HandleType(type, routeTypeReference, stateTypeReference, vecTypeReference, logger);
		return didAnything;
	}

	private bool HandleType(TypeDefinition type, TypeReference routeTypeReference, TypeReference stateTypeReference, TypeReference vecTypeReference, Action<AssemblyEditorResult.Message> logger)
	{
		var didAnything = false;
		RewriteOverride();
		RewriteCalls();
		return didAnything;

		void RewriteOverride()
		{
			if (!IsCorrectSubclass())
				return;

			foreach (var method in type.Methods)
			{
				if (method.Name != nameof(MapNodeContents.MakeRoute))
					continue;
				if (method.Parameters.Count != 1)
					continue;
				if (method.ReturnType.FullName != routeTypeReference.FullName)
					continue;
				if (method.Parameters[0].ParameterType.FullName != stateTypeReference.FullName)
					continue;
				
				logger(new() { Level = AssemblyEditorResult.MessageLevel.Debug, Content = $"Rewriting method definition `{method.FullName}` with an extra `{vecTypeReference.FullName}` parameter." });
				method.Parameters.Add(new ParameterDefinition(type.Module.ImportReference(vecTypeReference)));
				didAnything = true;
			}
			
			bool IsCorrectSubclass()
			{
				var currentType = type;
				while (true)
				{
					if (currentType is null)
						return false;
					if (currentType.Name == nameof(MapNodeContents))
						return true;
					currentType = currentType.BaseType?.Resolve();
				}
			}
		}

		void RewriteCalls()
		{
			foreach (var method in type.Methods)
				didAnything |= this.HandleMethod(method, routeTypeReference, stateTypeReference, logger);
		}
	}

	private bool HandleMethod(MethodDefinition method, TypeReference routeTypeReference, TypeReference stateTypeReference, Action<AssemblyEditorResult.Message> logger)
	{
		if (!method.HasBody)
			return false;

		var didAnything = false;
		
		var instructions = method.Body.Instructions;
		foreach (var instruction in instructions)
		{
			if (instruction.OpCode != COpCodes.Call && instruction.OpCode != COpCodes.Callvirt)
				continue;
			if (instruction.Operand is not MethodReference methodReference)
				continue;
			if (!methodReference.DeclaringType.Scope.Name.StartsWith("CobaltCore"))
				continue;
			if (methodReference.DeclaringType.Name != nameof(MapNodeContents))
				continue;
			if (methodReference.Name != nameof(MapNodeContents.MakeRoute))
				continue;
			if (methodReference.Parameters.Count != 1)
				continue;
			if (methodReference.ReturnType.FullName != routeTypeReference.FullName)
				continue;
			if (methodReference.Parameters[0].ParameterType.FullName != stateTypeReference.FullName)
				continue;

			var facadeMethodReference = method.Module.ImportReference(typeof(RewriteFacades.V1_2).GetMethod(nameof(RewriteFacades.V1_2.MapNodeContents_MakeRoute)));
			logger(new() { Level = AssemblyEditorResult.MessageLevel.Debug, Content = $"Rewriting method call `{methodReference.FullName}` with `{facadeMethodReference.FullName}`." });
			instruction.OpCode = COpCodes.Call;
			instruction.Operand = facadeMethodReference;
			didAnything = true;
		}
		
		return didAnything;
	}
}
#pragma warning restore CS0618 // Type or member is obsolete

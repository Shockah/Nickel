// ReSharper disable InconsistentNaming
#pragma warning disable CS0618 // Type or member is obsolete
using Mono.Cecil;
using Mono.Cecil.Cil;
using Nanoray.PluginManager.Cecil;
using System.Linq;

namespace Nickel;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public static partial class RewriteFacades
{
	public static partial class V1_2
	{
		public static Route MapNodeContents_MakeRoute(MapNodeContents @this, State s)
			=> @this.MakeRoute(s, s.map.currentLocation);
	}
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

internal sealed class V1_2_MapNodeContents_MakeRoute_DefinitionEditor : IAssemblyDefinitionEditor
{
	public bool WillEditAssembly(string fileBaseName)
		=> fileBaseName != "CobaltCore.dll";
	
	public bool EditAssemblyDefinition(AssemblyDefinition definition)
	{
		var didAnything = false;
		foreach (var module in definition.Modules)
			didAnything |= this.HandleModule(module);
		return didAnything;
	}

	private bool HandleModule(ModuleDefinition module)
	{
		if (module.AssemblyReferences.FirstOrDefault(r => r.Name == "CobaltCore") is not { } cobaltCoreAssemblyNameReference)
			return false;
		
		var cobaltCoreAssemblyDefinition = module.AssemblyResolver.Resolve(cobaltCoreAssemblyNameReference);
		var routeTypeReference = cobaltCoreAssemblyDefinition.MainModule.GetType("Route");
		var stateTypeReference = cobaltCoreAssemblyDefinition.MainModule.GetType("State");
		var vecTypeReference = cobaltCoreAssemblyDefinition.MainModule.GetType("Vec");
		
		var didAnything = false;
		foreach (var type in module.Types)
			didAnything |= this.HandleType(type, routeTypeReference, stateTypeReference, vecTypeReference);
		return didAnything;
	}

	private bool HandleType(TypeDefinition type, TypeReference routeTypeReference, TypeReference stateTypeReference, TypeReference vecTypeReference)
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
				didAnything |= this.HandleMethod(method);
		}
	}

	private bool HandleMethod(MethodDefinition method)
	{
		if (!method.HasBody)
			return false;

		var didAnything = false;
		
		var instructions = method.Body.Instructions;
		foreach (var instruction in instructions)
		{
			if (instruction.OpCode != OpCodes.Call && instruction.OpCode != OpCodes.Callvirt)
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

			var facadeMethodReference = method.Module.ImportReference(typeof(RewriteFacades.V1_2).GetMethod(nameof(RewriteFacades.V1_2.MapNodeContents_MakeRoute)));
			instruction.OpCode = OpCodes.Call;
			instruction.Operand = facadeMethodReference;
			didAnything = true;
		}
		
		return didAnything;
	}
}
#pragma warning restore CS0618 // Type or member is obsolete

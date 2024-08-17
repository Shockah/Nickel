using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Nanoray.PluginManager.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class GameFieldToPropertyDefinitionEditor : IAssemblyDefinitionEditor
{
	private readonly Dictionary<string, PropertyDefinition?> FieldToPropertyCache = [];
	
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
		if (module.AssemblyReferences.All(r => r.Name != "CobaltCore"))
			return false;
		
		var didAnything = false;
		foreach (var type in module.Types)
			didAnything |= this.HandleType(type, module.AssemblyResolver);
		return didAnything;
	}

	private bool HandleType(TypeDefinition type, IAssemblyResolver assemblyResolver)
	{
		var didAnything = false;
		foreach (var nestedType in type.NestedTypes)
			didAnything |= this.HandleType(nestedType, assemblyResolver);
		foreach (var method in type.Methods)
			didAnything |= this.HandleMethod(method, assemblyResolver);
		return didAnything;
	}

	private bool HandleMethod(MethodDefinition method, IAssemblyResolver assemblyResolver)
	{
		if (!method.HasBody)
			return false;
		
		var didAnything = false;
		for (var i = 0; i < method.Body.Instructions.Count; i++)
		{
			var instruction = method.Body.Instructions[i];
			if (instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Ldflda || instruction.OpCode == OpCodes.Ldsfld || instruction.OpCode == OpCodes.Ldsflda)
				didAnything |= this.HandleFieldLoadInstruction(ref instruction, assemblyResolver);
			else if (instruction.OpCode == OpCodes.Stfld || instruction.OpCode == OpCodes.Stsfld)
				didAnything |= this.HandleFieldStoreInstruction(ref instruction, assemblyResolver);
			
			method.Body.Instructions[i] = instruction;
		}
		return didAnything;
	}

	private PropertyDefinition? GetPropertyForField(FieldReference fieldReference)
	{
		var cacheKey = fieldReference.FullName;
		if (this.FieldToPropertyCache.TryGetValue(cacheKey, out var propertyReference))
			return propertyReference;
		if (fieldReference.Resolve() != null)
		{
			/* field exists */
			return this.FieldToPropertyCache[cacheKey] = null;
		}
		if (fieldReference.DeclaringType.Resolve() is not { } targetType)
		{
			/* declaring type does not exist */
			return this.FieldToPropertyCache[cacheKey] = null;
		}
		var property = targetType.Properties.FirstOrDefault(x => x.Name == fieldReference.Name);
		return this.FieldToPropertyCache[cacheKey] = property;
	}

	private bool HandleFieldLoadInstruction(ref Mono.Cecil.Cil.Instruction instruction, IAssemblyResolver assemblyResolver)
	{
		if (instruction.Operand is not FieldReference fieldReference)
			return false;
		if (!fieldReference.DeclaringType.Scope.Name.StartsWith("CobaltCore"))
			return false;

		var property = this.GetPropertyForField(fieldReference);
		if (property == null) return false;

		instruction.OpCode = OpCodes.Call;
		instruction.Operand = fieldReference.DeclaringType.Module.ImportReference(property.GetMethod);
		return true;
	}

	private bool HandleFieldStoreInstruction(ref Mono.Cecil.Cil.Instruction instruction, IAssemblyResolver assemblyResolver)
	{
		if (instruction.Operand is not FieldReference fieldReference)
			return false;
		if (!fieldReference.DeclaringType.Scope.Name.StartsWith("CobaltCore"))
			return false;

		var property = this.GetPropertyForField(fieldReference);
		if (property == null) return false;

		instruction.OpCode = OpCodes.Call;
		instruction.Operand = fieldReference.DeclaringType.Module.ImportReference(property.SetMethod);
		return true;
	}
}

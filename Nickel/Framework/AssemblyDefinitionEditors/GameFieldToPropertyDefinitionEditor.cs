using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Nanoray.PluginManager.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace Nickel;

internal sealed class GameFieldToPropertyDefinitionEditor : IAssemblyDefinitionEditor
{
	private readonly Dictionary<FieldReference, MethodReference?> FieldToGetterReferenceCache = [];
	private readonly Dictionary<FieldReference, MethodReference?> FieldToSetterReferenceCache = [];
	
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

	private bool HandleFieldLoadInstruction(ref Mono.Cecil.Cil.Instruction instruction, IAssemblyResolver assemblyResolver)
	{
		if (instruction.Operand is not FieldReference fieldReference)
			return false;
		if (!fieldReference.DeclaringType.Scope.Name.StartsWith("CobaltCore"))
			return false;

		if (!this.FieldToGetterReferenceCache.TryGetValue(fieldReference, out var getterReference))
		{
			if (fieldReference.DeclaringType.Scope is not AssemblyNameReference scopeAssemblyName)
			{
				this.FieldToGetterReferenceCache[fieldReference] = null;
				return false;
			}
			if (assemblyResolver.Resolve(scopeAssemblyName) is not { } scopeAssembly)
			{
				this.FieldToGetterReferenceCache[fieldReference] = null;
				return false;
			}
			if (scopeAssembly.MainModule.ImportReference(fieldReference.DeclaringType) is not TypeDefinition containingType)
			{
				this.FieldToGetterReferenceCache[fieldReference] = null;
				return false;
			}
			if (containingType.GetMethods().FirstOrDefault(m => m.Name == $"get_{fieldReference.Name}") is not { } getterDefinition)
			{
				this.FieldToGetterReferenceCache[fieldReference] = null;
				return false;
			}
			
			getterReference = getterDefinition.Module.ImportReference(getterDefinition);
			this.FieldToGetterReferenceCache[fieldReference] = getterReference;
		}

		instruction.OpCode = OpCodes.Call;
		instruction.Operand = getterReference;
		return true;
	}

	private bool HandleFieldStoreInstruction(ref Mono.Cecil.Cil.Instruction instruction, IAssemblyResolver assemblyResolver)
	{
		if (instruction.Operand is not FieldReference fieldReference)
			return false;
		if (!fieldReference.DeclaringType.Scope.Name.StartsWith("CobaltCore"))
			return false;

		if (!this.FieldToSetterReferenceCache.TryGetValue(fieldReference, out var setterReference))
		{
			if (fieldReference.DeclaringType.Scope is not AssemblyNameReference scopeAssemblyName)
			{
				this.FieldToSetterReferenceCache[fieldReference] = null;
				return false;
			}
			if (assemblyResolver.Resolve(scopeAssemblyName) is not { } scopeAssembly)
			{
				this.FieldToSetterReferenceCache[fieldReference] = null;
				return false;
			}
			if (scopeAssembly.MainModule.ImportReference(fieldReference.DeclaringType) is not TypeDefinition containingType)
			{
				this.FieldToSetterReferenceCache[fieldReference] = null;
				return false;
			}
			if (containingType.GetMethods().FirstOrDefault(m => m.Name == $"set_{fieldReference.Name}") is not { } setterDefinition)
			{
				this.FieldToSetterReferenceCache[fieldReference] = null;
				return false;
			}
			
			setterReference = setterDefinition.Module.ImportReference(setterDefinition);
			this.FieldToSetterReferenceCache[fieldReference] = setterReference;
		}

		instruction.OpCode = OpCodes.Call;
		instruction.Operand = setterReference;
		return true;
	}
}

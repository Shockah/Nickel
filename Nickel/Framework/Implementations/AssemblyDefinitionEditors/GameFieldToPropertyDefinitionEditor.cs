using Mono.Cecil;
using Mono.Cecil.Cil;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Nickel;

internal sealed class GameFieldToPropertyDefinitionEditor : IAssemblyDefinitionEditor
{
	private readonly Dictionary<string, MethodReference?> GetterCache = [];
	private readonly Dictionary<string, MethodReference?> SetterCache = [];
	
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
		if (module.AssemblyReferences.All(r => r.Name != "CobaltCore"))
			return false;
		
		var didAnything = false;
		foreach (var type in module.Types)
			didAnything |= this.HandleType(type, logger);
		return didAnything;
	}

	private bool HandleType(TypeDefinition type, Action<AssemblyEditorResult.Message> logger)
	{
		var didAnything = false;
		foreach (var nestedType in type.NestedTypes)
			didAnything |= this.HandleType(nestedType, logger);
		foreach (var method in type.Methods)
			didAnything |= this.HandleMethod(method, logger);
		return didAnything;
	}

	private bool HandleMethod(MethodDefinition method, Action<AssemblyEditorResult.Message> logger)
	{
		if (!method.HasBody)
			return false;
		
		var didAnything = false;
		var instructions = method.Body.Instructions;
		for (var i = 0; i < instructions.Count; i++)
		{
			var instruction = instructions[i];
			if (instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Ldflda || instruction.OpCode == OpCodes.Ldsfld || instruction.OpCode == OpCodes.Ldsflda)
				didAnything |= HandleFieldLoadInstruction(ref instruction, method, this.GetterCache, logger);
			else if (instruction.OpCode == OpCodes.Stfld || instruction.OpCode == OpCodes.Stsfld)
				didAnything |= HandleFieldStoreInstruction(ref instruction, method, this.SetterCache, logger);
			
			instructions[i] = instruction;
		}
		return didAnything;
	}

	private static bool HandleFieldLoadInstruction(ref Mono.Cecil.Cil.Instruction instruction, MethodDefinition method, Dictionary<string, MethodReference?> cache, Action<AssemblyEditorResult.Message> logger)
	{
		if (instruction.Operand is not FieldReference fieldReference)
			return false;
		if (fieldReference.DeclaringType.Scope.Name != "CobaltCore")
			return false;
		
		ref var getterReference = ref CollectionsMarshal.GetValueRefOrAddDefault(cache, fieldReference.FullName, out var getterReferenceExists);
		if (!getterReferenceExists)
		{
			if (fieldReference.DeclaringType.Resolve() is not { } containingType)
			{
				cache[fieldReference.FullName] = null;
				return false;
			}
			if (containingType.Fields.Any(f => f.Name == fieldReference.Name))
			{
				cache[fieldReference.FullName] = null;
				return false;
			}
			if (containingType.Properties.FirstOrDefault(p => p.Name == fieldReference.Name) is not { } propertyDefinition || propertyDefinition.GetMethod is null)
			{
				cache[fieldReference.FullName] = null;
				return false;
			}
			
			getterReference = method.Module.ImportReference(propertyDefinition.GetMethod);
			logger(new() { Level = AssemblyEditorResult.MessageLevel.Debug, Content = $"Rewriting field `{fieldReference.FullName}` read in {method.FullName} to {getterReference.FullName}." });
		}
		
		if (getterReference is null)
			return false;

		instruction.OpCode = OpCodes.Call;
		instruction.Operand = getterReference;
		return true;
	}

	private static bool HandleFieldStoreInstruction(ref Mono.Cecil.Cil.Instruction instruction, MethodDefinition method, Dictionary<string, MethodReference?> cache, Action<AssemblyEditorResult.Message> logger)
	{
		if (instruction.Operand is not FieldReference fieldReference)
			return false;
		if (fieldReference.DeclaringType.Scope.Name != "CobaltCore")
			return false;
		
		ref var setterReference = ref CollectionsMarshal.GetValueRefOrAddDefault(cache, fieldReference.FullName, out var setterReferenceExists);
		if (!setterReferenceExists)
		{
			if (fieldReference.DeclaringType.Resolve() is not { } containingType)
			{
				cache[fieldReference.FullName] = null;
				return false;
			}
			if (containingType.Fields.Any(f => f.Name == fieldReference.Name))
			{
				cache[fieldReference.FullName] = null;
				return false;
			}
			if (containingType.Properties.FirstOrDefault(p => p.Name == fieldReference.Name) is not { } propertyDefinition || propertyDefinition.SetMethod is null)
			{
				cache[fieldReference.FullName] = null;
				return false;
			}
			
			setterReference = method.Module.ImportReference(propertyDefinition.SetMethod);
			logger(new() { Level = AssemblyEditorResult.MessageLevel.Debug, Content = $"Rewriting field `{fieldReference.FullName}` write in `{method.FullName}` to {setterReference.FullName}." });
		}

		if (setterReference is null)
			return false;

		instruction.OpCode = OpCodes.Call;
		instruction.Operand = setterReference;
		return true;
	}
}

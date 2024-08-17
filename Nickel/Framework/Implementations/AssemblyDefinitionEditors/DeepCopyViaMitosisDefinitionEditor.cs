using Mono.Cecil;
using Mono.Cecil.Cil;
using Nanoray.PluginManager.Cecil;
using System.Linq;

namespace Nickel;

internal sealed class DeepCopyViaMitosisDefinitionEditor : IAssemblyDefinitionEditor
{
	public bool WillEditAssembly(string fileBaseName)
		=> fileBaseName == "CobaltCore.dll";

	public bool EditAssemblyDefinition(AssemblyDefinition definition)
	{
		var mutilType = definition.MainModule.GetType("Mutil");
		var mutilDeepCopyMethod = mutilType.Methods.First(m => m.Name == "DeepCopy");
		var nickelStaticDeepCopyMethod = definition.MainModule.ImportReference(typeof(NickelStatic).GetMethod("DeepCopy"));
		
		var genericNickelStaticDeepCopyMethod = new GenericInstanceMethod(nickelStaticDeepCopyMethod);
		genericNickelStaticDeepCopyMethod.GenericArguments.Add(mutilDeepCopyMethod.GenericParameters.First());

		var body = new MethodBody(mutilDeepCopyMethod);
		var il = body.GetILProcessor();
		
		il.Emit(OpCodes.Ldarg_0);
		il.Emit(OpCodes.Call, genericNickelStaticDeepCopyMethod);
		il.Emit(OpCodes.Ret);
		
		mutilDeepCopyMethod.Body = body;
		return true;
	}
}

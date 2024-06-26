using Mono.Cecil;
using Nanoray.PluginManager.Cecil;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Nickel;

internal sealed class NoInliningDefinitionEditor : IAssemblyDefinitionEditor
{
	public bool WillEditAssembly(string fileBaseName)
		=> fileBaseName == "CobaltCore.dll";

	public void EditAssemblyDefinition(AssemblyDefinition definition)
	{
		var methodImplAttributeCtor = definition.MainModule.ImportReference(typeof(MethodImplAttribute).GetConstructor([typeof(MethodImplOptions)]));
		var methodImplOptionsTypeReference = definition.MainModule.ImportReference(typeof(MethodImplOptions));

		AddAttribute(definition.MainModule.GetType("Log").Methods.Single(m => m.Name == "Line"));
		AddAttribute(definition.MainModule.GetType("Card").Methods.Single(m => m.Name == "Key"));
		AddAttribute(definition.MainModule.GetType("Card").Methods.Single(m => m.Name == "GetMeta"));
		AddAttribute(definition.MainModule.GetType("Artifact").Methods.Single(m => m.Name == "Key"));
		AddAttribute(definition.MainModule.GetType("Artifact").Methods.Single(m => m.Name == "GetMeta"));
		AddAttribute(definition.MainModule.GetType("CardAction").Methods.Single(m => m.Name == "Key"));
		AddAttribute(definition.MainModule.GetType("FightModifier").Methods.Single(m => m.Name == "Key"));
		AddAttribute(definition.MainModule.GetType("MapBase").Methods.Single(m => m.Name == "Key"));

		void AddAttribute(MethodDefinition methodDefinition)
		{
			if (!methodDefinition.IsStatic)
				methodDefinition.IsVirtual = true;
			var attribute = new CustomAttribute(methodImplAttributeCtor);
			attribute.ConstructorArguments.Add(new CustomAttributeArgument(methodImplOptionsTypeReference, MethodImplOptions.NoInlining));
			methodDefinition.CustomAttributes.Add(attribute);
		}
	}
}

using Mono.Cecil;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;
using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Nickel;

internal sealed class NoInliningDefinitionEditor : IAssemblyDefinitionEditor
{
	public bool WillEditAssembly(string fileBaseName)
		=> fileBaseName == "CobaltCore.dll";

	public bool EditAssemblyDefinition(AssemblyDefinition definition, Action<AssemblyEditorResult.Message> logger)
	{
		var methodImplAttributeCtor = definition.MainModule.ImportReference(typeof(MethodImplAttribute).GetConstructor([typeof(MethodImplOptions)]));
		var methodImplOptionsTypeReference = definition.MainModule.ImportReference(typeof(MethodImplOptions));
		
		AddAttribute(nameof(Artifact), nameof(Artifact.Key));
		AddAttribute(nameof(Artifact), nameof(Artifact.GetMeta));
		AddAttribute(nameof(Card), nameof(Card.Key));
		AddAttribute(nameof(Card), nameof(Card.GetMeta));
		AddAttribute(nameof(CardAction), nameof(CardAction.Key));
		AddAttribute(nameof(FightModifier), nameof(FightModifier.Key));
		AddAttribute(nameof(Log), nameof(Log.Line));
		AddAttribute(nameof(MapBase), nameof(MapBase.Key));

		return true;

		void AddAttribute(string typeName, string methodName)
		{
			var methodDefinition = definition.MainModule.GetType(typeName).Methods.Single(m => m.Name == methodName);
			if (!methodDefinition.IsStatic)
				methodDefinition.IsVirtual = true;
			var attribute = new CustomAttribute(methodImplAttributeCtor);
			attribute.ConstructorArguments.Add(new CustomAttributeArgument(methodImplOptionsTypeReference, MethodImplOptions.NoInlining));
			methodDefinition.CustomAttributes.Add(attribute);
		}
	}
}

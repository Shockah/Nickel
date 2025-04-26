using Mono.Cecil;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Nickel;

internal sealed class ModDataFieldDefinitionEditor : IAssemblyDefinitionEditor
{
	internal const string FieldName = "__Nickel__ModData";
	internal const string JsonPropertyName = "ModData";
	
	internal static readonly IReadOnlyList<string> TypeNamesToAddFieldTo = [
		nameof(Card),
		nameof(CardAction),
		nameof(Route),
		nameof(StuffBase),
		nameof(Artifact),
		nameof(State),
	];
	
	public byte[] AssemblyEditorDescriptor
		=> Encoding.UTF8.GetBytes($"{this.GetType().FullName}, {NickelConstants.Name} {NickelConstants.Version}");
	
	public bool WillEditAssembly(string fileBaseName)
		=> fileBaseName == "CobaltCore.dll";

	public bool EditAssemblyDefinition(AssemblyDefinition definition, Action<AssemblyEditorResult.Message> logger)
	{
		var fieldTypeReference = definition.MainModule.ImportReference(typeof(Dictionary<string, Dictionary<string, object?>>));
		var stringTypeReference = definition.MainModule.ImportReference(typeof(string));
		var attributeCtor = definition.MainModule.ImportReference(typeof(JsonPropertyAttribute).GetConstructor([typeof(string)]));
		
		foreach (var typeName in TypeNamesToAddFieldTo)
		{
			var type = definition.MainModule.GetType(typeName);
			var field = new FieldDefinition(FieldName, FieldAttributes.Private, fieldTypeReference);

			var attribute = new CustomAttribute(attributeCtor);
			attribute.ConstructorArguments.Add(new CustomAttributeArgument(stringTypeReference, JsonPropertyName));
			field.CustomAttributes.Add(attribute);
			
			type.Fields.Add(field);
		}

		return true;
	}
}

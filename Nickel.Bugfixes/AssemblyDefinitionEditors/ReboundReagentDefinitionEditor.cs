using Mono.Cecil;
using Mono.Cecil.Rocks;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;

namespace Nickel.Bugfixes;

internal sealed class ReboundReagentDefinitionEditor : IAssemblyDefinitionEditor
{
	public byte[] AssemblyEditorDescriptor
		=> Encoding.UTF8.GetBytes($"{this.GetType().FullName}, {ModEntry.Instance.Package.Manifest.UniqueName} {ModEntry.Instance.Package.Manifest.Version}");
	
	public bool WillEditAssembly(string fileBaseName)
		=> fileBaseName == "CobaltCore.dll";

	public bool EditAssemblyDefinition(AssemblyDefinition definition, Action<AssemblyEditorResult.Message> logger)
	{
		var jsonPropertyAttributeType = definition.MainModule.ImportReference(typeof(JsonPropertyAttribute)).Resolve();
		var jsonPropertyAttributeCtor = jsonPropertyAttributeType.GetConstructors().First(ctor => ctor.Parameters.Count == 0);
		var artifactType = definition.MainModule.GetType(nameof(ReboundReagent));
		var alreadyActivatedField = artifactType.Fields.First(p => p.Name == nameof(ReboundReagent.alreadyActivated));
		
		if (alreadyActivatedField.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.Name == nameof(JsonIgnoreAttribute)) is { } jsonIgnoreAttribute)
			alreadyActivatedField.CustomAttributes.Remove(jsonIgnoreAttribute);

		var jsonPropertyAttribute = new CustomAttribute(definition.MainModule.ImportReference(jsonPropertyAttributeCtor));
		alreadyActivatedField.CustomAttributes.Add(jsonPropertyAttribute);
		
		return true;
	}
}

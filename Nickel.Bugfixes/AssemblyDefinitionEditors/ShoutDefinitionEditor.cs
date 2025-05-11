using Mono.Cecil;
using Mono.Cecil.Rocks;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Text;

namespace Nickel.Bugfixes;

internal sealed class ShoutDefinitionEditor : IAssemblyDefinitionEditor
{
	public byte[] AssemblyEditorDescriptor
		=> Encoding.UTF8.GetBytes($"{this.GetType().FullName}, {ModEntry.Instance.Package.Manifest.UniqueName} {ModEntry.Instance.Package.Manifest.Version}");
	
	public bool WillEditAssembly(string fileBaseName)
		=> fileBaseName == "CobaltCore.dll";

	public bool EditAssemblyDefinition(AssemblyDefinition definition, Action<AssemblyEditorResult.Message> logger)
	{
		var jsonPropertyAttributeType = definition.MainModule.ImportReference(typeof(JsonPropertyAttribute)).Resolve();
		var jsonPropertyAttributeCtor = jsonPropertyAttributeType.GetConstructors().First(ctor => ctor.Parameters.Count == 0);
		var shoutType = definition.MainModule.GetType(nameof(Shout));
		
		HandleField(nameof(Shout._lastLocaleText));
		HandleField(nameof(Shout._textCache));
		
		return true;

		void HandleField(string fieldName)
		{
			var field = shoutType.Fields.First(p => p.Name == fieldName);
		
			if (field.CustomAttributes.FirstOrDefault(attr => attr.AttributeType.Name == nameof(JsonIgnoreAttribute)) is { } jsonIgnoreAttribute)
				field.CustomAttributes.Remove(jsonIgnoreAttribute);

			var jsonPropertyAttribute = new CustomAttribute(definition.MainModule.ImportReference(jsonPropertyAttributeCtor));
			field.CustomAttributes.Add(jsonPropertyAttribute);
		}
	}
}

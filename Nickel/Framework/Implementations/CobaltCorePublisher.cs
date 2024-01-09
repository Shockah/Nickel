using Mono.Cecil;
using Nanoray.PluginManager.Cecil;
using Newtonsoft.Json;

namespace Nickel;

public class CobaltCorePublisher : IAssemblyDefinitionEditor
{
	public bool WillEditAssembly(string assemblyName) => assemblyName == "CobaltCore.dll";

	public void EditAssemblyDefinition(AssemblyDefinition definition)
	{
		var jsonIgnoreCtor = definition.MainModule.ImportReference(typeof(JsonIgnoreAttribute).GetConstructor([]));
		foreach(var type in definition.MainModule.Types)
		{
			type.IsPublic = true;
			PublishContents(type, jsonIgnoreCtor);
		}
	}

	private static void PublishContents(TypeDefinition type, MethodReference jsonIgnoreCtor)
	{
		/* Blindly setting everything to true would result in the JSON-serialization process
		 * (used for save games and stat saving) saving a _bunch_ of data that it isn't supposed to.
		 *
		 * To counteract this, we add `[JsonIgnore]` as appropriate.
		 */

		foreach (var field in type.Fields)
		{
			if (!field.IsPublic)
			{
				field.CustomAttributes.Add(new CustomAttribute(jsonIgnoreCtor));
				field.IsPublic = true;
			}
		}

		foreach (var property in type.Properties)
		{
			if (!property.GetMethod.IsPublic && !property.SetMethod.IsPublic)
			{
				property.CustomAttributes.Add(new CustomAttribute(jsonIgnoreCtor));
			}
		}

		foreach (var method in type.Methods)
			method.IsPublic = true;

		foreach (var nestedType in type.NestedTypes)
		{
			nestedType.IsNestedPublic = true;
			PublishContents(nestedType, jsonIgnoreCtor);
		}
	}
}

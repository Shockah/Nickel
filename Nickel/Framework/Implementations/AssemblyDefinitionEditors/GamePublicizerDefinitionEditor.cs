using Mono.Cecil;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;
using Newtonsoft.Json;
using System;

namespace Nickel;

internal sealed class GamePublicizerDefinitionEditor : IAssemblyDefinitionEditor
{
	public bool WillEditAssembly(string fileBaseName)
		=> fileBaseName == "CobaltCore.dll";

	public bool EditAssemblyDefinition(AssemblyDefinition definition, Action<AssemblyEditorResult.Message> logger)
	{
		var jsonIgnoreCtor = definition.MainModule.ImportReference(typeof(JsonIgnoreAttribute).GetConstructor([]));
		var didAnything = false;
		foreach (var type in definition.MainModule.Types)
		{
			if (!type.IsPublic)
			{
				didAnything = true;
				type.IsPublic = true;
			}
			
			didAnything |= PublishContents(type, jsonIgnoreCtor);
		}
		return didAnything;
	}

	private static bool PublishContents(TypeDefinition type, MethodReference jsonIgnoreCtor)
	{
		/* Blindly setting everything to true would result in the JSON-serialization process
		 * (used for save games and stat saving) saving a _bunch_ of data that it isn't supposed to.
		 *
		 * To counteract this, we add `[JsonIgnore]` as appropriate.
		 */

		var didAnything = false;

		foreach (var field in type.Fields)
		{
			if (field.IsPublic)
				continue;

			didAnything = true;
			field.CustomAttributes.Add(new CustomAttribute(jsonIgnoreCtor));
			field.IsPublic = true;
		}

		foreach (var property in type.Properties)
		{
			if (property.GetMethod?.IsPublic == true || property.SetMethod?.IsPublic == true)
				continue;
			property.CustomAttributes.Add(new CustomAttribute(jsonIgnoreCtor));
		}

		foreach (var method in type.Methods)
		{
			if (!method.IsPublic)
			{
				didAnything = true;
				method.IsPublic = true;
			}
		}

		foreach (var nestedType in type.NestedTypes)
		{
			if (!nestedType.IsNestedPublic)
			{
				didAnything = true;
				nestedType.IsNestedPublic = true;
			}
			
			didAnything |= PublishContents(nestedType, jsonIgnoreCtor);
		}

		return didAnything;
	}
}

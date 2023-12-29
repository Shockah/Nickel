using System.Collections.Generic;
using System.IO;
using Mono.Cecil;

namespace Nanoray.PluginManager.Cecil;

public sealed class ExtendableAssemblyDefinitionEditor : IAssemblyEditor
{
	private List<IAssemblyDefinitionEditor> DefinitionEditors { get; init; } = new();

	public Stream EditAssemblyStream(string name, Stream assemblyStream)
	{
		if (this.DefinitionEditors.Count <= 0)
			return assemblyStream;

		var definition = AssemblyDefinition.ReadAssembly(assemblyStream);
		foreach (var definitionEditor in this.DefinitionEditors)
			definitionEditor.EditAssemblyDefinition(definition);

		MemoryStream newStream = new();
		definition.Write(newStream);
		newStream.Position = 0;
		return newStream;
	}

	public void RegisterDefinitionEditor(IAssemblyDefinitionEditor definitionEditor)
		=> this.DefinitionEditors.Add(definitionEditor);

	public void UnregisterDefinitionEditor(IAssemblyDefinitionEditor definitionEditor)
		=> this.DefinitionEditors.Remove(definitionEditor);
}

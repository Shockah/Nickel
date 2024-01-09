using Mono.Cecil;
using System.Collections.Generic;
using System.IO;

namespace Nanoray.PluginManager.Cecil;

public sealed class ExtendableAssemblyDefinitionEditor : IAssemblyEditor
{
	private readonly List<IAssemblyDefinitionEditor> definitionEditors = [];

	public Stream EditAssemblyStream(string name, Stream assemblyStream)
	{
		if (this.definitionEditors.Count <= 0)
			return assemblyStream;

		var definition = AssemblyDefinition.ReadAssembly(assemblyStream);
		foreach (var definitionEditor in this.definitionEditors)
			definitionEditor.EditAssemblyDefinition(definition);

		MemoryStream newStream = new();
		definition.Write(newStream);
		newStream.Position = 0;
		return newStream;
	}

	public void RegisterDefinitionEditor(IAssemblyDefinitionEditor definitionEditor)
		=> this.definitionEditors.Add(definitionEditor);

	public void UnregisterDefinitionEditor(IAssemblyDefinitionEditor definitionEditor)
		=> this.definitionEditors.Remove(definitionEditor);
}

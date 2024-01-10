using Mono.Cecil;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nanoray.PluginManager.Cecil;

public sealed class ExtendableAssemblyDefinitionEditor : IAssemblyEditor
{
	private readonly List<IAssemblyDefinitionEditor> DefinitionEditors = [];

	public Stream EditAssemblyStream(string name, Stream assemblyStream)
	{
		var interestedEditors = this.DefinitionEditors.Where(x => x.WillEditAssembly(name)).ToList();
		if (interestedEditors.Count <= 0)
			return assemblyStream;

		var definition = AssemblyDefinition.ReadAssembly(assemblyStream);
		foreach (var definitionEditor in interestedEditors)
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

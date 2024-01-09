using Mono.Cecil;

namespace Nanoray.PluginManager.Cecil;

public interface IAssemblyDefinitionEditor
{
	bool WillEditAssembly(string fileBaseName);
	void EditAssemblyDefinition(AssemblyDefinition definition);
}

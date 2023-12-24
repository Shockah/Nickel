using Mono.Cecil;

namespace Nanoray.PluginManager.Cecil;

public interface IAssemblyDefinitionEditor
{
    void EditAssemblyDefinition(AssemblyDefinition definition);
}

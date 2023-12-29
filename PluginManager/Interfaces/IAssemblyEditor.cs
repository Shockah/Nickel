using System.IO;

namespace Nanoray.PluginManager;

public interface IAssemblyEditor
{
	Stream EditAssemblyStream(string name, Stream assemblyStream);
}

using System.Collections.Generic;
using System.Reflection;

namespace Nanoray.PluginManager;

public interface IAssemblyPluginLoaderReferenceResolver<TPluginManifest>
{
	IEnumerable<IFileInfo> ResolveAssemblyReferences(IPluginPackage<TPluginManifest> package, Assembly assembly);
}

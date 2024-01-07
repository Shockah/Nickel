using System.Runtime.Loader;

namespace Nanoray.PluginManager;

public interface IAssemblyPluginLoaderLoadContextProvider<TPluginManifest>
{
	AssemblyLoadContext GetLoadContext(IPluginPackage<TPluginManifest> package);
}

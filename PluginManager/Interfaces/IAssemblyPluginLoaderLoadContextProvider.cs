using System.Runtime.Loader;

namespace Nanoray.PluginManager;

public interface IAssemblyPluginLoaderLoadContextProvider<in TPluginManifest>
{
	AssemblyLoadContext GetLoadContext(IPluginPackage<TPluginManifest> package);
}

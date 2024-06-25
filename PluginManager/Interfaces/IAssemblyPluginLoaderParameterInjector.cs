using System;

namespace Nanoray.PluginManager;

public interface IAssemblyPluginLoaderParameterInjector<in TPluginManifest>
{
	bool TryToInjectParameter(IPluginPackage<TPluginManifest> package, Type type, out object? toInject);
}

using System;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

public sealed class ExtendableAssemblyPluginLoaderParameterInjector<TPluginManifest> : IAssemblyPluginLoaderParameterInjector<TPluginManifest>
{
	private List<IAssemblyPluginLoaderParameterInjector<TPluginManifest>> ParameterInjectors { get; } = [];

	public bool TryToInjectParameter(IPluginPackage<TPluginManifest> package, Type type, out object? toInject)
	{
		foreach (var injector in this.ParameterInjectors)
			if (injector.TryToInjectParameter(package, type, out toInject))
				return true;
		toInject = null;
		return false;
	}

	public void RegisterParameterInjector(IAssemblyPluginLoaderParameterInjector<TPluginManifest> injector)
		=> this.ParameterInjectors.Add(injector);

	public void UnregisterParameterInjector(IAssemblyPluginLoaderParameterInjector<TPluginManifest> injector)
		=> this.ParameterInjectors.Remove(injector);
}

using System;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

public sealed class CompoundAssemblyPluginLoaderParameterInjector<TPluginManifest> : IAssemblyPluginLoaderParameterInjector<TPluginManifest>
{
	private IReadOnlyList<IAssemblyPluginLoaderParameterInjector<TPluginManifest>> ParameterInjectors { get; }

	public CompoundAssemblyPluginLoaderParameterInjector(IReadOnlyList<IAssemblyPluginLoaderParameterInjector<TPluginManifest>> parameterInjectors)
	{
		this.ParameterInjectors = parameterInjectors;
	}

	public CompoundAssemblyPluginLoaderParameterInjector(params IAssemblyPluginLoaderParameterInjector<TPluginManifest>[] parameterInjectors) : this((IReadOnlyList<IAssemblyPluginLoaderParameterInjector<TPluginManifest>>)parameterInjectors) { }

	public bool TryToInjectParameter(IPluginPackage<TPluginManifest> package, Type type, out object? toInject)
	{
		foreach (var injector in this.ParameterInjectors)
			if (injector.TryToInjectParameter(package, type, out toInject))
				return true;
		toInject = null;
		return false;
	}
}

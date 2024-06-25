using System;

namespace Nanoray.PluginManager;

public sealed class ValueAssemblyPluginLoaderParameterInjector<TPluginManifest, T> : IAssemblyPluginLoaderParameterInjector<TPluginManifest>
{
	private T Value { get; }

	public ValueAssemblyPluginLoaderParameterInjector(T value)
	{
		this.Value = value;
	}

	public bool TryToInjectParameter(IPluginPackage<TPluginManifest> package, Type type, out object? toInject)
	{
		if (type.IsInstanceOfType(this.Value))
		{
			toInject = this.Value;
			return true;
		}
		toInject = null;
		return false;
	}
}

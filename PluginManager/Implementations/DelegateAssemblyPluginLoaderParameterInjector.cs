using System;

namespace Nanoray.PluginManager;

public sealed class DelegateAssemblyPluginLoaderParameterInjector<TPluginManifest, T>
	: IAssemblyPluginLoaderParameterInjector<TPluginManifest>
{
	private Func<IPluginPackage<TPluginManifest>, T> Delegate { get; }

	public DelegateAssemblyPluginLoaderParameterInjector(Func<IPluginPackage<TPluginManifest>, T> @delegate)
	{
		this.Delegate = @delegate;
	}

	public bool TryToInjectParameter(IPluginPackage<TPluginManifest> package, Type type, out object? toInject)
	{
		if (type.IsAssignableFrom(typeof(T)))
		{
			toInject = this.Delegate(package);
			return true;
		}

		if (type.IsAssignableFrom(typeof(Func<IPluginPackage<TPluginManifest>, T>)))
		{
			toInject = this.Delegate;
			return true;
		}

		toInject = null;
		return false;
	}
}

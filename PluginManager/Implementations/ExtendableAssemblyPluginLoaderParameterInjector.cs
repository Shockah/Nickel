using System;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IAssemblyPluginLoaderParameterInjector{TPluginManifest}"/> which allows registering additional <see cref="IAssemblyPluginLoaderParameterInjector{TPluginManifest}"/> implementations.
/// Each implementation is invoked sequentially, until one succeeds.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
public sealed class ExtendableAssemblyPluginLoaderParameterInjector<TPluginManifest> : IAssemblyPluginLoaderParameterInjector<TPluginManifest>
{
	private readonly List<IAssemblyPluginLoaderParameterInjector<TPluginManifest>> ParameterInjectors = [];

	/// <inheritdoc/>
	public bool TryToInjectParameter(IPluginPackage<TPluginManifest> package, Type type, out object? toInject)
	{
		foreach (var injector in this.ParameterInjectors)
			if (injector.TryToInjectParameter(package, type, out toInject))
				return true;
		toInject = null;
		return false;
	}

	/// <summary>
	/// Register a parameter injector.
	/// </summary>
	/// <param name="injector">The parameter injector.</param>
	public void RegisterParameterInjector(IAssemblyPluginLoaderParameterInjector<TPluginManifest> injector)
		=> this.ParameterInjectors.Add(injector);

	/// <summary>
	/// Unregister a parameter injector.
	/// </summary>
	/// <param name="injector">The parameter injector.</param>
	public void UnregisterParameterInjector(IAssemblyPluginLoaderParameterInjector<TPluginManifest> injector)
		=> this.ParameterInjectors.Remove(injector);
}

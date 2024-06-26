using System;
using System.Collections.Generic;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IAssemblyPluginLoaderParameterInjector{TPluginManifest}"/> that attemps to inject a parameter with multiple underlying <see cref="IAssemblyPluginLoaderParameterInjector{TPluginManifest}"/> implementations.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
public sealed class CompoundAssemblyPluginLoaderParameterInjector<TPluginManifest> : IAssemblyPluginLoaderParameterInjector<TPluginManifest>
{
	private IReadOnlyList<IAssemblyPluginLoaderParameterInjector<TPluginManifest>> ParameterInjectors { get; }

	/// <summary>
	/// Creates a new <see cref="CompoundAssemblyPluginLoaderParameterInjector{TPluginManifest}"/>.
	/// </summary>
	/// <param name="parameterInjectors">The underlying parameter injectors.</param>
	public CompoundAssemblyPluginLoaderParameterInjector(IReadOnlyList<IAssemblyPluginLoaderParameterInjector<TPluginManifest>> parameterInjectors)
	{
		this.ParameterInjectors = parameterInjectors;
	}

	/// <inheritdoc/>
	public bool TryToInjectParameter(IPluginPackage<TPluginManifest> package, Type type, out object? toInject)
	{
		foreach (var injector in this.ParameterInjectors)
			if (injector.TryToInjectParameter(package, type, out toInject))
				return true;
		toInject = null;
		return false;
	}
}

using System;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IAssemblyPluginLoaderParameterInjector{TPluginManifest}"/> which injects a constant value.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="T">The type of value that is being injected</typeparam>
public sealed class ValueAssemblyPluginLoaderParameterInjector<TPluginManifest, T> : IAssemblyPluginLoaderParameterInjector<TPluginManifest>
{
	private readonly T Value;

	/// <summary>
	/// Creates a new <see cref="ValueAssemblyPluginLoaderParameterInjector{TPluginManifest,T}"/>.
	/// </summary>
	/// <param name="value">The constant value to inject.</param>
	public ValueAssemblyPluginLoaderParameterInjector(T value)
	{
		this.Value = value;
	}

	/// <inheritdoc/>
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

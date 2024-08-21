using System;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IAssemblyPluginLoaderParameterInjector{TPluginManifest}"/> which injects a value provided via a delegate function.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="T">The type of value that is being injected</typeparam>
public sealed class DelegateAssemblyPluginLoaderParameterInjector<TPluginManifest, T>
	: IAssemblyPluginLoaderParameterInjector<TPluginManifest>
{
	private readonly Func<IPluginPackage<TPluginManifest>, T> Delegate;

	/// <summary>
	/// Creates a new <see cref="DelegateAssemblyPluginLoaderParameterInjector{TPluginManifest,T}"/>
	/// </summary>
	/// <param name="delegate">The delegate function which provides a value to inject.</param>
	public DelegateAssemblyPluginLoaderParameterInjector(Func<IPluginPackage<TPluginManifest>, T> @delegate)
	{
		this.Delegate = @delegate;
	}

	/// <inheritdoc/>
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

using System;
using System.Diagnostics.CodeAnalysis;

namespace Nanoray.PluginManager;

public sealed class ValidatingAssemblyPluginLoaderParameterInjector<TPluginManifest>(
	IAssemblyPluginLoaderParameterInjector<TPluginManifest> injector,
	Func<IPluginPackage<TPluginManifest>, Type, bool> validator
) : IAssemblyPluginLoaderParameterInjector<TPluginManifest>
{
	private IAssemblyPluginLoaderParameterInjector<TPluginManifest> Injector { get; } = injector;
	private Func<IPluginPackage<TPluginManifest>, Type, bool> Validator { get; } = validator;

	public bool TryToInjectParameter(
		IPluginPackage<TPluginManifest> package,
		Type type,
		[MaybeNullWhen(false)] out object? toInject
	)
	{
		if (!this.Validator(package, type))
		{
			toInject = null;
			return false;
		}
		return this.Injector.TryToInjectParameter(package, type, out toInject);
	}
}

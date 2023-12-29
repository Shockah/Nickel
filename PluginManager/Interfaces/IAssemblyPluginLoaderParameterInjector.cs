using System;
using System.Diagnostics.CodeAnalysis;

namespace Nanoray.PluginManager;

public interface IAssemblyPluginLoaderParameterInjector<in TPluginManifest>
{
	bool TryToInjectParameter(IPluginPackage<TPluginManifest> package, Type type, [MaybeNullWhen(false)] out object? toInject);
}

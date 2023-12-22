using System;
using System.Diagnostics.CodeAnalysis;

namespace Shockah.PluginManager;

public sealed class DelegateAssemblyPluginLoaderParameterInjector<TPluginManifest, T> : IAssemblyPluginLoaderParameterInjector<TPluginManifest>
{
    private Func<IPluginPackage<TPluginManifest>, T> Delegate { get; init; }

    public DelegateAssemblyPluginLoaderParameterInjector(Func<IPluginPackage<TPluginManifest>, T> @delegate)
    {
        this.Delegate = @delegate;
    }

    public bool TryToInjectParameter(IPluginPackage<TPluginManifest> package, Type type, [MaybeNullWhen(false)] out object? toInject)
    {
        if (type.IsAssignableFrom(typeof(T)))
        {
            toInject = this.Delegate(package);
            return true;
        }
        toInject = null;
        return false;
    }
}

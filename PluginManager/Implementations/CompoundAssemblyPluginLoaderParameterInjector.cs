using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Shockah.PluginManager;

public sealed class CompoundAssemblyPluginLoaderParameterInjector : IAssemblyPluginLoaderParameterInjector
{
    private IReadOnlyList<IAssemblyPluginLoaderParameterInjector> ParameterInjectors { get; init; }

    public CompoundAssemblyPluginLoaderParameterInjector(IReadOnlyList<IAssemblyPluginLoaderParameterInjector> parameterInjectors)
    {
        this.ParameterInjectors = parameterInjectors;
    }

    public CompoundAssemblyPluginLoaderParameterInjector(params IAssemblyPluginLoaderParameterInjector[] parameterInjectors) : this((IReadOnlyList<IAssemblyPluginLoaderParameterInjector>)parameterInjectors) { }

    public bool TryToInjectParameter(Type type, [MaybeNullWhen(false)] out object? toInject)
    {
        foreach (var injector in this.ParameterInjectors)
            if (injector.TryToInjectParameter(type, out toInject))
                return true;
        toInject = null;
        return false;
    }
}

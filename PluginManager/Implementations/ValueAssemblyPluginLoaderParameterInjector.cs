using System;
using System.Diagnostics.CodeAnalysis;

namespace Shockah.PluginManager;

public sealed class ValueAssemblyPluginLoaderParameterInjector<T> : IAssemblyPluginLoaderParameterInjector
{
    private T Value { get; init; }

    public ValueAssemblyPluginLoaderParameterInjector(T value)
    {
        this.Value = value;
    }

    public bool TryToInjectParameter(Type type, [MaybeNullWhen(false)] out object? toInject)
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

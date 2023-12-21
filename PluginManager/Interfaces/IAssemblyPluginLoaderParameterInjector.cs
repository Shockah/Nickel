using System;
using System.Diagnostics.CodeAnalysis;

namespace Shockah.PluginManager;

public interface IAssemblyPluginLoaderParameterInjector
{
    bool TryToInjectParameter(Type type, [MaybeNullWhen(false)] out object? toInject);
}

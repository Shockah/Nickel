using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using OneOf;
using OneOf.Types;

namespace Shockah.PluginManager;

public sealed class AssemblyPluginLoader<TPluginManifest, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
{
    private Func<IPluginPackage<TPluginManifest>, RequiredPluginData?> RequiredPluginDataProvider { get; init; }
    private IAssemblyPluginLoaderParameterInjector? ParameterInjector { get; init; }

    public AssemblyPluginLoader(Func<IPluginPackage<TPluginManifest>, RequiredPluginData?> requiredPluginDataProvider, IAssemblyPluginLoaderParameterInjector? parameterInjector)
    {
        this.RequiredPluginDataProvider = requiredPluginDataProvider;
        this.ParameterInjector = parameterInjector;
    }

    public bool CanLoadPlugin(IPluginPackage<TPluginManifest> package)
        => this.RequiredPluginDataProvider(package) is not null;

    public OneOf<LoadedPluginInfo<TPluginManifest, TPlugin>, Error<string>> LoadPlugin(IPluginPackage<TPluginManifest> package)
    {
        if (this.RequiredPluginDataProvider(package) is not { } requiredPluginData)
            throw new ArgumentException($"This plugin loader cannot load the plugin package {package}.");
        using var stream = package.GetDataStream(requiredPluginData.EntryPointAssemblyFileName);

        AssemblyLoadContext context = new(requiredPluginData.UniqueName);
        var assembly = context.LoadFromStream(stream);
        var pluginTypes = assembly.GetTypes().Where(t => t.IsAssignableTo(typeof(TPlugin))).ToList();

        if (pluginTypes.Count <= 0)
            return new Error<string>($"The assembly {assembly} in package {package} does not include any {typeof(TPlugin)} subclasses.");
        if (pluginTypes.Count > 1)
            return new Error<string>($"The assembly {assembly} in package {package} includes multiple {typeof(TPlugin)} subclasses: {string.Join(", ", pluginTypes.Select(t => t.FullName))}.");
        var pluginType = pluginTypes[0];

        var parameterInjector = new CompoundAssemblyPluginLoaderParameterInjector(
            new IAssemblyPluginLoaderParameterInjector?[]
            {
                new ValueAssemblyPluginLoaderParameterInjector<TPluginManifest>(package.Manifest),
                new ValueAssemblyPluginLoaderParameterInjector<IPluginPackage<TPluginManifest>>(package),
                this.ParameterInjector
            }
            .OfType<IAssemblyPluginLoaderParameterInjector>()
            .ToList()
        );
        var potentialConstructors = pluginType.GetConstructors()
            .Select(c =>
            {
                var constructorParameters = c.GetParameters();
                var injectedParameters = new InjectedParameter?[constructorParameters.Length];
                for (int i = 0; i < constructorParameters.Length; i++)
                    injectedParameters[i] = parameterInjector.TryToInjectParameter(constructorParameters[i].ParameterType, out object? toInject) ? new InjectedParameter { Value = toInject } : null;
                return new PotentialConstructor { Constructor = c, Parameters = injectedParameters };
            })
            .OrderByDescending(c => c.Parameters.Length)
            .ToList();

        if (!potentialConstructors.Any(c => c.IsValid))
            return new Error<string>($"The type {pluginType} in assembly {assembly} in package {package} has a constructor with parameters which could not be injected: {string.Join(", ", potentialConstructors.First().Constructor.GetParameters().Select(p => p.Name))}.");
        var constructor = potentialConstructors.Where(c => c.IsValid).First();

        object? rawPlugin = constructor.Constructor.Invoke(constructor.Parameters.Select(p => (object)p!.Value).ToArray());
        if (rawPlugin is not TPlugin plugin)
            return new Error<string>($"Could not construct a {typeof(TPlugin)} subclass from the type {pluginType} in assembly {assembly} in package {package}.");
        return new LoadedPluginInfo<TPluginManifest, TPlugin>(package, plugin);
    }

    public readonly struct RequiredPluginData
    {
        public string UniqueName { get; }
        public string EntryPointAssemblyFileName { get; }
    }

    private readonly struct PotentialConstructor
    {
        public ConstructorInfo Constructor { get; init; }
        public InjectedParameter?[] Parameters { get; init; }

        public bool IsValid
            => this.Constructor.GetParameters().Length == this.Parameters.Length && this.Parameters.All(p => p is not null);
    }

    private readonly struct InjectedParameter
    {
        public object? Value { get; init; }
    }
}

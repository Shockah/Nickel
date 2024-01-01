using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Nanoray.PluginManager;

public sealed class AssemblyPluginLoader<TPluginManifest, TPluginPart, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
{
	private Func<IPluginPackage<TPluginManifest>, AssemblyPluginLoaderRequiredPluginData?> RequiredPluginDataProvider { get; }
	private IAssemblyPluginLoaderPartAssembler<TPluginManifest, TPluginPart, TPlugin> PartAssembler { get; }
	private IAssemblyPluginLoaderParameterInjector<TPluginManifest>? ParameterInjector { get; }
	private IAssemblyEditor? AssemblyEditor { get; }

	public AssemblyPluginLoader(
		Func<IPluginPackage<TPluginManifest>, AssemblyPluginLoaderRequiredPluginData?> requiredPluginDataProvider,
		IAssemblyPluginLoaderPartAssembler<TPluginManifest, TPluginPart, TPlugin> partAssembler,
		IAssemblyPluginLoaderParameterInjector<TPluginManifest>? parameterInjector,
		IAssemblyEditor? assemblyEditor
	)
	{
		this.RequiredPluginDataProvider = requiredPluginDataProvider;
		this.PartAssembler = partAssembler;
		this.ParameterInjector = parameterInjector;
		this.AssemblyEditor = assemblyEditor;
	}

	public bool CanLoadPlugin(IPluginPackage<TPluginManifest> package)
		=> this.RequiredPluginDataProvider(package) is not null;

	public OneOf<TPlugin, Error<string>> LoadPlugin(IPluginPackage<TPluginManifest> package)
	{
		if (this.RequiredPluginDataProvider(package) is not { } requiredPluginData)
			throw new ArgumentException($"This plugin loader cannot load the plugin package {package}.");

		using var originalStream = package.PackageRoot.GetRelativeFile(requiredPluginData.EntryPointAssembly).OpenRead();
		using var stream = this.AssemblyEditor?.EditAssemblyStream(requiredPluginData.EntryPointAssembly, originalStream) ?? originalStream;

		AssemblyLoadContext context = new(requiredPluginData.UniqueName);
		var assembly = context.LoadFromStream(stream);
		HashSet<Type> pluginPartTypes;

		if (string.IsNullOrEmpty(requiredPluginData.EntryPointType))
		{
			pluginPartTypes = assembly.GetTypes()
				.Where(t => !t.IsAbstract)
				.Where(t => t.IsAssignableTo(typeof(TPluginPart)))
				.ToHashSet();
		}
		else
		{
			var pluginTypeOrNull = assembly.GetType(requiredPluginData.EntryPointType);
			if (pluginTypeOrNull is null)
				return new Error<string>($"The type `{requiredPluginData.EntryPointType}` in assembly {assembly} does not exist.");
			if (pluginTypeOrNull.IsAbstract || !pluginTypeOrNull.IsAssignableTo(typeof(TPluginPart)))
				return new Error<string>($"The type `{requiredPluginData.EntryPointType}` in assembly {assembly} is not a valid {typeof(TPluginPart)} subclass.");
			pluginPartTypes = [pluginTypeOrNull];
		}

		if (this.PartAssembler.ValidatePluginParts(package, assembly, pluginPartTypes) is { } partAssemblerValidationError)
			return partAssemblerValidationError;

		var parameterInjector = new CompoundAssemblyPluginLoaderParameterInjector<TPluginManifest>(
			new IAssemblyPluginLoaderParameterInjector<TPluginManifest>?[]
			{
				new ValueAssemblyPluginLoaderParameterInjector<TPluginManifest, TPluginManifest>(package.Manifest),
				new ValueAssemblyPluginLoaderParameterInjector<TPluginManifest, IPluginPackage<TPluginManifest>>(package),
				this.ParameterInjector
			}
			.OfType<IAssemblyPluginLoaderParameterInjector<TPluginManifest>>()
			.ToList()
		);

		HashSet<TPluginPart> pluginParts = [];
		foreach (var pluginPartType in pluginPartTypes)
		{
			var potentialConstructors = pluginPartType.GetConstructors()
				.Select(c =>
				{
					var constructorParameters = c.GetParameters();
					var injectedParameters = new InjectedParameter?[constructorParameters.Length];
					for (var i = 0; i < constructorParameters.Length; i++)
						injectedParameters[i] = parameterInjector.TryToInjectParameter(package, constructorParameters[i].ParameterType, out var toInject) ? new InjectedParameter { Value = toInject } : null;
					return new PotentialConstructor { Constructor = c, Parameters = injectedParameters };
				})
				.OrderByDescending(c => c.Parameters.Length)
				.ToList();

			if (!potentialConstructors.Any(c => c.IsValid))
				return new Error<string>($"The type {pluginPartType} in assembly {assembly} has a constructor with parameters which could not be injected: {string.Join(", ", potentialConstructors.First().Constructor.GetParameters().Select(p => p.Name))}.");
			var constructor = potentialConstructors.Where(c => c.IsValid).First();

			var parameters = constructor.Parameters.Select(p => p?.Value!).ToArray();
			var rawPluginPart = constructor.Constructor.Invoke(parameters: parameters);
			if (rawPluginPart is not TPluginPart pluginPart)
				return new Error<string>($"Could not construct a {typeof(TPluginPart)} subclass from the type {pluginPartType} in assembly {assembly} in package {package}.");
			pluginParts.Add(pluginPart);
		}

		return this.PartAssembler.AssemblePluginParts(package, assembly, pluginParts);
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

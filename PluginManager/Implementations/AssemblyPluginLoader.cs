using OneOf;
using OneOf.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nanoray.PluginManager;

/// <summary>
/// An <see cref="IPluginLoader{TPluginManifest,TPlugin}"/> which loads <see cref="Assembly">Assemblies</see> as plugins.
/// </summary>
/// <typeparam name="TPluginManifest">The type of the plugin manifest.</typeparam>
/// <typeparam name="TPluginPart">The plugin part type.</typeparam>
/// <typeparam name="TPlugin">The plugin type.</typeparam>
public sealed class AssemblyPluginLoader<TPluginManifest, TPluginPart, TPlugin> : IPluginLoader<TPluginManifest, TPlugin>
{
	private Func<IPluginPackage<TPluginManifest>, OneOf<AssemblyPluginLoaderRequiredPluginData, Error<string>>> RequiredPluginDataProvider { get; }
	private IAssemblyPluginLoaderLoadContextProvider<TPluginManifest> LoadContextProvider { get; }
	private IAssemblyPluginLoaderPartAssembler<TPluginManifest, TPluginPart, TPlugin> PartAssembler { get; }
	private IAssemblyPluginLoaderParameterInjector<TPluginManifest>? ParameterInjector { get; }
	private IAssemblyEditor? AssemblyEditor { get; }

	/// <summary>
	/// Creates a new <see cref="AssemblyPluginLoader{TPluginManifest,TPluginPart,TPlugin}"/>.
	/// </summary>
	/// <param name="requiredPluginDataProvider">A function which maps plugin manifests to the data required for this loader.</param>
	/// <param name="loadContextProvider">A load context provider.</param>
	/// <param name="partAssembler">A part assembler.</param>
	/// <param name="parameterInjector">An optional parameter injector.</param>
	/// <param name="assemblyEditor">An optional assembly editor.</param>
	public AssemblyPluginLoader(
		Func<IPluginPackage<TPluginManifest>, OneOf<AssemblyPluginLoaderRequiredPluginData, Error<string>>> requiredPluginDataProvider,
		IAssemblyPluginLoaderLoadContextProvider<TPluginManifest> loadContextProvider,
		IAssemblyPluginLoaderPartAssembler<TPluginManifest, TPluginPart, TPlugin> partAssembler,
		IAssemblyPluginLoaderParameterInjector<TPluginManifest>? parameterInjector,
		IAssemblyEditor? assemblyEditor
	)
	{
		this.RequiredPluginDataProvider = requiredPluginDataProvider;
		this.LoadContextProvider = loadContextProvider;
		this.PartAssembler = partAssembler;
		this.ParameterInjector = parameterInjector;
		this.AssemblyEditor = assemblyEditor;
	}

	/// <inheritdoc/>
	public OneOf<Yes, No, Error<string>> CanLoadPlugin(IPluginPackage<TPluginManifest> package)
		=> this.RequiredPluginDataProvider(package).Match<OneOf<Yes, No, Error<string>>>(
			_ => new Yes(),
			error => error
		);

	/// <inheritdoc/>
	public PluginLoadResult<TPlugin> LoadPlugin(IPluginPackage<TPluginManifest> package)
	{
		if (!this.RequiredPluginDataProvider(package).TryPickT0(out var requiredPluginData, out _))
			throw new ArgumentException($"This plugin loader cannot load the plugin package {package}.");

		Assembly assembly;
		try
		{
			var assemblyFile = package.PackageRoot.GetRelativeFile(requiredPluginData.EntryPointAssembly);
			using var originalStream = assemblyFile.OpenRead();
			var refStream = originalStream;

			this.AssemblyEditor?.EditAssemblyStream(assemblyFile.Name, ref refStream);
			using var stream = refStream;

			var assemblyName = requiredPluginData.EntryPointAssembly;
			if (assemblyName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
				assemblyName = assemblyName[..^4];

			var context = this.LoadContextProvider.GetLoadContext(package);
			assembly = context.LoadFromAssemblyName(new AssemblyName(assemblyName));
		}
		catch (Exception ex)
		{
			return new Error<string>($"There was a problem while loading assemblies: {ex}");
		}

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
			new[]
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
			var constructor = potentialConstructors.First(c => c.IsValid);

			var parameters = constructor.Parameters.Select(p => p?.Value!).ToArray();
			var rawPluginPart = constructor.Constructor.Invoke(parameters: parameters);
			if (rawPluginPart is not TPluginPart pluginPart)
				return new Error<string>($"Could not construct a {typeof(TPluginPart)} subclass from the type {pluginPartType} in assembly {assembly} in package {package}.");
			pluginParts.Add(pluginPart);
		}

		return this.PartAssembler.AssemblePluginParts(package, assembly, pluginParts).Match<PluginLoadResult<TPlugin>>(
			plugin => new PluginLoadResult<TPlugin>.Success { Plugin = plugin, Warnings = [] },
			error => error
		);
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

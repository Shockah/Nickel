using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;

namespace Nickel;

internal sealed class CobaltCoreAssemblyResolver(CobaltCoreResolveResult? resolveResult) : IAssemblyResolver
{
	private readonly Dictionary<string, AssemblyDefinition> AssemblyDefinitions = [];
	
	public void Dispose()
	{
		foreach (var definition in this.AssemblyDefinitions.Values)
			definition.Dispose();
		this.AssemblyDefinitions.Clear();
	}

	public AssemblyDefinition Resolve(AssemblyNameReference name)
		=> this.Resolve(name, new ReaderParameters());

	public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
	{
		if (this.AssemblyDefinitions.TryGetValue(name.Name, out var definition))
			return definition;
		if (resolveResult is null)
			throw new AssemblyResolutionException(name);

		Func<Stream> assemblyStreamProvider;
		Func<Stream>? symbolsStreamProvider;

		if (name.Name == "CobaltCore")
		{
			assemblyStreamProvider = resolveResult.Value.GameAssemblyDataStreamProvider;
			symbolsStreamProvider = resolveResult.Value.GameSymbolsDataStreamProvider;
		}
		else if (resolveResult.Value.OtherDllDataStreamProviders.TryGetValue($"{name.Name}.dll", out var otherDllStreamProvider))
		{
			assemblyStreamProvider = otherDllStreamProvider;
			symbolsStreamProvider = null;
		}
		else
		{
			throw new AssemblyResolutionException(name);
		}
		
		parameters.AssemblyResolver ??= this;
		parameters.SymbolStream ??= symbolsStreamProvider?.Invoke();
		
		definition = ModuleDefinition.ReadModule(assemblyStreamProvider(), parameters).Assembly;
		this.AssemblyDefinitions[name.Name] = definition;
		return definition;
	}
}

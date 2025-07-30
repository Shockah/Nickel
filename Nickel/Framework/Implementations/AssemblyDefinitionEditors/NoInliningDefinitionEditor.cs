using Mono.Cecil;
using Nanoray.PluginManager;
using Nanoray.PluginManager.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Nickel;

internal sealed class NoInliningDefinitionEditor(
	Func<IModManifest> modLoaderManifestProvider,
	Func<IEnumerable<IAssemblyModManifest>> manifestProvider
) : IAssemblyDefinitionEditor
{
	private record StopInliningCompiledDefinition(
		IModManifest OnBehalfOf,
		Regex AssemblyName,
		Regex TypeName,
		Regex MethodName,
		int? ArgumentCount,
		bool IgnoreNoMatches
	)
	{
		public int TimesMatched;
	}
	
	private readonly Lazy<List<StopInliningCompiledDefinition>> Definitions = new(() =>
	{
		var modLoaderManifest = modLoaderManifestProvider();
		List<StopInliningDefinition> modLoaderDefinitions = [
			new() { TypeName = nameof(Artifact), MethodName = nameof(Artifact.Key) },
			new() { TypeName = nameof(Artifact), MethodName = nameof(Artifact.GetMeta) },
			new() { TypeName = nameof(Card), MethodName = nameof(Card.Key) },
			new() { TypeName = nameof(Card), MethodName = nameof(Card.GetMeta) },
			new() { TypeName = nameof(CardAction), MethodName = nameof(CardAction.Key) },
			new() { TypeName = nameof(FightModifier), MethodName = nameof(FightModifier.Key) },
			new() { TypeName = nameof(Log), MethodName = nameof(Log.Line) },
			new() { TypeName = nameof(MapBase), MethodName = nameof(MapBase.Key) },
			new() { TypeName = nameof(State), MethodName = nameof(State.EnumerateAllArtifacts) },
			new() { TypeName = nameof(State), MethodName = nameof(State.UpdateArtifactCache) },
		];
		
		List<(IModManifest Manifest, StopInliningDefinition Definition)> modLoaderDefinitionsOnBehalfOf = modLoaderDefinitions
			.Select(d => (modLoaderManifest, d))
			.ToList();

		List<(IModManifest Manifest, StopInliningDefinition Definition)> modDefinitionsOnBehalfOf = manifestProvider()
			.SelectMany(m => m.MethodsToStopInlining.Select(d => ((IModManifest)m, d)))
			.ToList();

		return modLoaderDefinitionsOnBehalfOf.Concat(modDefinitionsOnBehalfOf)
			.Select(d =>
			{
				var assemblyName = new Regex($"^{(string.IsNullOrEmpty(d.Definition.AssemblyName) ? "CobaltCore" : d.Definition.AssemblyName)}$");
				var typeName = new Regex($"^{d.Definition.TypeName}$");
				var methodName = new Regex($"^{d.Definition.MethodName}$");
				return new StopInliningCompiledDefinition(d.Manifest, assemblyName, typeName, methodName, d.Definition.ArgumentCount, d.Definition.IgnoreNoMatches);
			})
			.ToList();
	});
	
	public byte[] AssemblyEditorDescriptor
		=> Encoding.UTF8.GetBytes($"{this.GetType().FullName}, {NickelConstants.Name} {NickelConstants.Version}");

	public bool WillEditAssembly(string fileBaseName)
	{
		if (fileBaseName.EndsWith(".dll"))
			fileBaseName = fileBaseName[..^4];
		return this.Definitions.Value.Any(d => d.AssemblyName.IsMatch(fileBaseName));
	}
	
	public bool EditAssemblyDefinition(AssemblyDefinition definition, Action<AssemblyEditorResult.Message> logger)
	{
		var matchingDefinitions = this.Definitions.Value.Where(d => d.AssemblyName.IsMatch(definition.Name.Name)).ToList();
		if (matchingDefinitions.Count == 0)
			return false;
		
		var didAnything = false;
		foreach (var module in definition.Modules)
			didAnything |= HandleModule(module, logger, matchingDefinitions);
		
		foreach (var matchingDefinition in matchingDefinitions)
			if (matchingDefinition is { TimesMatched: 0, IgnoreNoMatches: false })
				logger(new() { Level = AssemblyEditorResult.MessageLevel.Error, Content = $"Requested by {matchingDefinition.OnBehalfOf.GetDisplayName(false)} to rewrite: {{Assembly: `{matchingDefinition.AssemblyName}`, Type: `{matchingDefinition.TypeName}`, Method: `{matchingDefinition.MethodName}`}}, but found no candidates." });
		
		return didAnything;
	}

	private static bool HandleModule(ModuleDefinition module, Action<AssemblyEditorResult.Message> logger, List<StopInliningCompiledDefinition> matchingDefinitions)
	{
		var didAnything = false;
		foreach (var type in module.Types)
			didAnything |= HandleType(type, logger, matchingDefinitions);
		return didAnything;
	}

	private static bool HandleType(TypeDefinition type, Action<AssemblyEditorResult.Message> logger, List<StopInliningCompiledDefinition> matchingDefinitions)
	{
		matchingDefinitions = matchingDefinitions.Where(d => d.TypeName.IsMatch(type.FullName)).ToList();
		if (matchingDefinitions.Count == 0)
			return false;
		
		var didAnything = false;
		foreach (var nestedType in type.NestedTypes)
			didAnything |= HandleType(nestedType, logger, matchingDefinitions);
		foreach (var method in type.Methods)
			didAnything |= HandleMethod(method, logger, matchingDefinitions);
		return didAnything;
	}

	private static bool HandleMethod(MethodDefinition method, Action<AssemblyEditorResult.Message> logger, List<StopInliningCompiledDefinition> matchingDefinitions)
	{
		if (!method.HasBody)
			return false;
		
		matchingDefinitions = matchingDefinitions.Where(d => (d.ArgumentCount is null || d.ArgumentCount.Value == method.Parameters.Count) && d.MethodName.IsMatch(method.Name)).ToList();
		if (matchingDefinitions.Count == 0)
			return false;
		
		foreach (var matchingDefinition in matchingDefinitions)
			matchingDefinition.TimesMatched++;
		logger(new() { Level = AssemblyEditorResult.MessageLevel.Debug, Content = $"Rewriting method {method.FullName} to stop it from being inlined, requested by {matchingDefinitions[0].OnBehalfOf.GetDisplayName(false)}." });
		method.ImplAttributes |= MethodImplAttributes.NoInlining;
		
		return true;
	}
}

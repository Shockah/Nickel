using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Nickel.ModBuildConfig.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DirectGameEnumUsageAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

	private readonly Dictionary<DiagnosticSeverity, DiagnosticDescriptor> DirectGameEnumUsageRule;

	public DirectGameEnumUsageAnalyzer()
	{
		this.DirectGameEnumUsageRule = new List<DiagnosticSeverity>
		{
			DiagnosticSeverity.Error, DiagnosticSeverity.Warning, DiagnosticSeverity.Info
		}.ToDictionary(s => s, s => new DiagnosticDescriptor(
			id: "DirectGameEnumUsage",
			title: "Avoid direct game enum usage",
			messageFormat: "'{0}' is a Cobalt Core enum; directly referencing it may cause unexpected behavior when compiled against a different game version than is currently running; consider using `Enum.TryParse<{0}>(\"{1}\")` or the EnumByNameSourceGenerator NuGet package instead",
			category: "Nickel.CommonErrors",
			defaultSeverity: s,
			isEnabledByDefault: true
		));
			
		this.SupportedDiagnostics = [
			.. this.DirectGameEnumUsageRule.Values
		];
	}
	
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
		context.EnableConcurrentExecution();
		
		context.RegisterSyntaxNodeAction(
			this.Analyze,
			SyntaxKind.SimpleMemberAccessExpression
		);
	}

	private void Analyze(SyntaxNodeAnalysisContext context)
	{
		var options = ParseOptions(context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.FilterTree));
		
		var expression = (MemberAccessExpressionSyntax)context.Node;
		var declaringType = context.SemanticModel.GetTypeInfo(expression.Expression).Type;

		if (declaringType is not INamedTypeSymbol { TypeKind: TypeKind.Enum } namedTypeSymbol)
			return;
		if (!declaringType.ContainingNamespace.IsGlobalNamespace)
			return;
		if (!declaringType.ContainingAssembly.Name.StartsWith("CobaltCore"))
			return;
		if (namedTypeSymbol.GetMembers().OfType<IFieldSymbol>().All(s => s.Name != expression.Name.ToString()))
			return;
		if (GetSeverity() is not { } severity)
			return;
		if (IsInAttribute(expression))
			return;

		context.ReportDiagnostic(Diagnostic.Create(this.DirectGameEnumUsageRule[severity], context.Node.GetLocation(), declaringType, expression.Name));

		bool IsInAttribute(SyntaxNode node)
		{
			while (true)
			{
				if (node.Parent is not { } parent)
					return false;
				if (parent is AttributeArgumentSyntax)
					return true;
				node = parent;
			}
		}

		DiagnosticSeverity? GetSeverity()
		{
			if (options.ErrorEnums.Contains(declaringType.Name) || options.ErrorEnums.Contains("*"))
				return DiagnosticSeverity.Error;
			if (options.WarningEnums.Contains(declaringType.Name) || options.WarningEnums.Contains("*"))
				return DiagnosticSeverity.Warning;
			if (options.InfoEnums.Contains(declaringType.Name) || options.InfoEnums.Contains("*"))
				return DiagnosticSeverity.Info;
			if (options.IgnoredEnums.Contains(declaringType.Name) || options.IgnoredEnums.Contains("*"))
				return null;
			return options.DefaultSeverity;
		}
	}

	private static Options ParseOptions(AnalyzerConfigOptions options)
	{
		var errorEnums = new[] { "Spr", "UK" };
		var warningEnums = Array.Empty<string>();
		var infoEnums = Array.Empty<string>();
		var ignoredEnums = Array.Empty<string>();
		DiagnosticSeverity? defaultSeverity = DiagnosticSeverity.Error;

		if (options.TryGetValue("mod_build_config.direct_game_enum_usage.error_enums", out var rawErrorEnums))
			errorEnums = string.IsNullOrEmpty(rawErrorEnums) ? [] : rawErrorEnums.Split(',').Select(s => s.Trim()).ToArray();
		if (options.TryGetValue("mod_build_config.direct_game_enum_usage.warning_enums", out var rawWarningEnums))
			warningEnums = string.IsNullOrEmpty(rawWarningEnums) ? [] : rawWarningEnums.Split(',').Select(s => s.Trim()).ToArray();
		if (options.TryGetValue("mod_build_config.direct_game_enum_usage.info_enums", out var rawInfoEnums))
			infoEnums = string.IsNullOrEmpty(rawInfoEnums) ? [] : rawInfoEnums.Split(',').Select(s => s.Trim()).ToArray();
		if (options.TryGetValue("mod_build_config.direct_game_enum_usage.ignored_enums", out var rawIgnoredEnums))
			ignoredEnums = string.IsNullOrEmpty(rawIgnoredEnums) ? [] : rawIgnoredEnums.Split(',').Select(s => s.Trim()).ToArray();

		if (options.TryGetValue("mod_build_config.direct_game_enum_usage.default_severity", out var rawDefaultSeverity))
		{
			if (rawDefaultSeverity.Equals("null", StringComparison.InvariantCultureIgnoreCase) || rawDefaultSeverity.Equals("none", StringComparison.InvariantCultureIgnoreCase))
				defaultSeverity = null;
			else if (Enum.TryParse<DiagnosticSeverity>(rawDefaultSeverity, out var parsedDefaultSeverity))
				defaultSeverity = parsedDefaultSeverity;
		}

		return new()
		{
			ErrorEnums = errorEnums,
			WarningEnums = warningEnums,
			InfoEnums = infoEnums,
			IgnoredEnums = ignoredEnums,
			DefaultSeverity = defaultSeverity,
		};
	}

	private struct Options
	{
		public string[] ErrorEnums;
		public string[] WarningEnums;
		public string[] InfoEnums;
		public string[] IgnoredEnums;
		public DiagnosticSeverity? DefaultSeverity;
	}
}

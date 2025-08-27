using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace Nickel.ModBuildConfig.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CardTraitFieldAssignmentAnalyzer : DiagnosticAnalyzer
{
	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

	private readonly DiagnosticDescriptor Rule = new(
		id: "CardTraitFieldAssignment",
		title: "Avoid direct card trait field assignment",
		messageFormat: "Setting `Card.{0}` directly may cause issues with Nickel's card trait handling; consider using `IModCards.SetCardTraitOverride` instead",
		category: "Nickel.CommonErrors",
		defaultSeverity: DiagnosticSeverity.Info,
		isEnabledByDefault: true
	);

	private static readonly string[] CardTraitFieldNames = [
		"temporaryOverride",
		"singleUseOverride",
		"exhaustOverride",
		"retainOverride",
		"buoyantOverride",
		"recycleOverride",
		"unplayableOverride",
		"exhaustOverrideIsPermanent",
		"retainOverrideIsPermanent",
		"buoyantOverrideIsPermanent",
		"recycleOverrideIsPermanent",
		"unplayableOverrideIsPermanent",
	];

	public CardTraitFieldAssignmentAnalyzer()
		=> this.SupportedDiagnostics = [this.Rule];
	
	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
		context.EnableConcurrentExecution();
		
		context.RegisterSyntaxNodeAction(
			this.Analyze,
			SyntaxKind.SimpleAssignmentExpression,
			SyntaxKind.AndAssignmentExpression,
			SyntaxKind.OrAssignmentExpression,
			SyntaxKind.ExclusiveOrAssignmentExpression
		);
	}

	private void Analyze(SyntaxNodeAnalysisContext context)
	{
		if (GetContainedCardTraitFieldName(context.Node) is not { } cardTraitFieldName)
			return;
		context.ReportDiagnostic(Diagnostic.Create(this.Rule, context.Node.GetLocation(), cardTraitFieldName));

		string? GetContainedCardTraitFieldName(SyntaxNode node)
		{
			if (node is MemberAccessExpressionSyntax memberAccessExpression)
			{
				var declaringType = context.SemanticModel.GetTypeInfo(memberAccessExpression.Expression).Type;
				if (declaringType?.Name == "Card" && declaringType.ContainingNamespace.IsGlobalNamespace && declaringType.ContainingAssembly.Name.StartsWith("CobaltCore") && CardTraitFieldNames.Contains(memberAccessExpression.Name.ToString()))
					return memberAccessExpression.Name.ToString();
			}
			
			foreach (var child in node.ChildNodes())
				if (GetContainedCardTraitFieldName(child) is { } cardTraitFieldName)
					return cardTraitFieldName;
			return null;
		}
	}
}

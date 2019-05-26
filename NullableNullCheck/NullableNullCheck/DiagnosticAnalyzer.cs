using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NullableNullCheck
{
  [DiagnosticAnalyzer(LanguageNames.CSharp)]
  public class NullableNullCheckAnalyzer : DiagnosticAnalyzer
  {
    public const string DiagnosticId = "NullableNullCheck";

    private static readonly LocalizableString Title = "The Nullable null check is performed with HasValue.";
    private static readonly LocalizableString MessageFormat = "The variable '{0}' is checked for null with HasValue.";
    private static readonly LocalizableString Description = "Null checks for Nullable types should be performed with != null. Making it uniform to reference types and improving readability.";
    private const string Category = "Readability";

    private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

    public override void Initialize(AnalysisContext context)
    {
      context.RegisterSemanticModelAction(SemanticAction);
    }

    private void SemanticAction(SemanticModelAnalysisContext context)
    {
      SemanticModel semanticModel = context.SemanticModel;
      var model = semanticModel.SyntaxTree.GetRoot();

      var walker = new Walker(semanticModel, context);
      walker.Visit(model);
    }

    class Walker : CSharpSyntaxWalker
    {
      private SemanticModel semanticModel;
      private readonly SemanticModelAnalysisContext context;

      public Walker(SemanticModel semanticModel, SemanticModelAnalysisContext context)
      {
        this.semanticModel = semanticModel;
        this.context = context;
      }

      public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
      {
        this.EvaluateMemberAccessExpression(node);

        base.VisitMemberAccessExpression(node);
      }

      private void EvaluateMemberAccessExpression(MemberAccessExpressionSyntax node)
      {
        var typeInfo = semanticModel.GetTypeInfo(node.Expression);
        var name = typeInfo.Type.Name;
        if (name != "Nullable")
        {
          return;
        }

        if (node.Name.Identifier.ValueText != "HasValue")
        {
          return;
        }

        var diagnostic = Diagnostic.Create(Rule, node.GetLocation(), node.Name);
        context.ReportDiagnostic(diagnostic);
      }
    }
  }
}

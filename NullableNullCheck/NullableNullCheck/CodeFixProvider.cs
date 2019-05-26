using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace NullableNullCheck
{
  [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullableNullCheckCodeFixProvider)), Shared]
  public class NullableNullCheckCodeFixProvider : CodeFixProvider
  {
    private const string title = "Use != null";

    public sealed override ImmutableArray<string> FixableDiagnosticIds
    {
      get { return ImmutableArray.Create(NullableNullCheckAnalyzer.DiagnosticId); }
    }

    public sealed override FixAllProvider GetFixAllProvider()
    {
      return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
      var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

      var diagnostic = context.Diagnostics.First();
      var diagnosticSpan = diagnostic.Location.SourceSpan;

      var declaration = root.FindToken(diagnosticSpan.Start).Parent.Parent.AncestorsAndSelf().OfType<MemberAccessExpressionSyntax>().First();

      context.RegisterCodeFix(
          CodeAction.Create(
              title: title,
              createChangedDocument: c => MakeUppercaseAsync(context.Document, declaration, c),
              equivalenceKey: title),
          diagnostic);
    }

    private async Task<Document> MakeUppercaseAsync(Document document, MemberAccessExpressionSyntax typeDecl, CancellationToken cancellationToken)
    {
      var root = await document.GetSyntaxRootAsync();

      var node = SyntaxFactory.BinaryExpression(
                                    SyntaxKind.NotEqualsExpression,
                                    typeDecl.Expression,
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.NullLiteralExpression)).NormalizeWhitespace();

      var newRoot = root.ReplaceNode(typeDecl, node);
      var newDocument = document.WithSyntaxRoot(newRoot);
      var identifierToken = typeDecl.Name.Identifier;
      return newDocument;
    }
  }
}
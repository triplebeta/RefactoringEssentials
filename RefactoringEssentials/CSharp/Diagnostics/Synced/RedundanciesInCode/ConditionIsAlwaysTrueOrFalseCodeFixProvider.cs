using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CodeFixes;

namespace RefactoringEssentials.CSharp.Diagnostics
{
    [ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
    public class ConditionIsAlwaysTrueOrFalseCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(DiagnosticIDs.ConditionIsAlwaysTrueOrFalseAnalyzerID);
            }
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public async override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var span = context.Span;
            var diagnostics = context.Diagnostics;
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var diagnostic = diagnostics.First();
            var node = root.FindNode(context.Span);
            bool isTrue = diagnostic.GetMessage().IndexOf("'true'", System.StringComparison.Ordinal) >= 0;
            var newRoot = root.ReplaceNode(
                node,
                SyntaxFactory.LiteralExpression(isTrue ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression)
                .WithLeadingTrivia(node.GetLeadingTrivia())
                .WithTrailingTrivia(node.GetTrailingTrivia()));
            context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, isTrue ? "Replace with 'true'" : "Replace with 'false'", document.WithSyntaxRoot(newRoot)), diagnostic);
        }
    }
}
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;

namespace RefactoringEssentials.CSharp.Diagnostics
{
    [ExportCodeFixProvider(LanguageNames.CSharp), System.Composition.Shared]
    public class ConvertConditionalTernaryToNullCoalescingCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(DiagnosticIDs.ConvertConditionalTernaryToNullCoalescingAnalyzerID);
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
            var model = await document.GetSemanticModelAsync(cancellationToken);
            var diagnostic = diagnostics.First();
            var node = root.FindNode(context.Span) as ConditionalExpressionSyntax;
            if (node == null)
                return;
            context.RegisterCodeFix(CodeActionFactory.Create(node.Span, diagnostic.Severity, "Replace '?:'  operator with '??", token =>
            {
                ExpressionSyntax a, other;
                if (node.Condition.SkipParens().IsKind(SyntaxKind.EqualsExpression))
                {
                    a = node.WhenFalse;
                    other = node.WhenTrue;
                }
                else
                {
                    other = node.WhenFalse;
                    a = node.WhenTrue;
                }

                if (node.Condition.SkipParens().IsKind(SyntaxKind.EqualsExpression))
                {
                    var castExpression = other as CastExpressionSyntax;
                    if (castExpression != null)
                    {
                        a = SyntaxFactory.CastExpression(castExpression.Type, a);
                        other = castExpression.Expression;
                    }
                }

                a = UnpackNullableValueAccess(model, a, token);

                ExpressionSyntax newNode = SyntaxFactory.BinaryExpression(SyntaxKind.CoalesceExpression, a, other);

                var newRoot = root.ReplaceNode((SyntaxNode)node, newNode.WithLeadingTrivia(node.GetLeadingTrivia()).WithAdditionalAnnotations(Formatter.Annotation));
                return Task.FromResult(document.WithSyntaxRoot(newRoot));
            }), diagnostic);
        }

        internal static ExpressionSyntax UnpackNullableValueAccess(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
        {
            var expr = expression.SkipParens();
            if (!expr.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                return expression;
            var info = semanticModel.GetTypeInfo(((MemberAccessExpressionSyntax)expr).Expression, cancellationToken);
            if (!info.ConvertedType.IsNullableType())
                return expression;
            return ((MemberAccessExpressionSyntax)expr).Expression;
        }
    }
}
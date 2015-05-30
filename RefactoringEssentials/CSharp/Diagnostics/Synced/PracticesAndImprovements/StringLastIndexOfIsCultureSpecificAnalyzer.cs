using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace RefactoringEssentials.CSharp.Diagnostics
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StringLastIndexOfIsCultureSpecificAnalyzer : DiagnosticAnalyzer
    {
        static readonly DiagnosticDescriptor descriptor = new DiagnosticDescriptor(
            DiagnosticIDs.StringLastIndexOfIsCultureSpecificAnalyzerID,
            GettextCatalog.GetString("Warns when a culture-aware 'LastIndexOf' call is used by default."),
            GettextCatalog.GetString("'LastIndexOf' is culture-aware and missing a StringComparison argument"),
            DiagnosticAnalyzerCategories.PracticesAndImprovements,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.CreateFor(DiagnosticIDs.StringLastIndexOfIsCultureSpecificAnalyzerID)
        );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(descriptor);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(
                (nodeContext) =>
                {
                    Diagnostic diagnostic;
                    if (TryGetDiagnostic(nodeContext, out diagnostic))
                        nodeContext.ReportDiagnostic(diagnostic);
                },
                SyntaxKind.InvocationExpression
            );
        }

        static bool TryGetDiagnostic(SyntaxNodeAnalysisContext nodeContext, out Diagnostic diagnostic)
        {
            diagnostic = default(Diagnostic);
            if (nodeContext.IsFromGeneratedCode())
                return false;
            var node = nodeContext.Node as InvocationExpressionSyntax;
            MemberAccessExpressionSyntax mre = node.Expression as MemberAccessExpressionSyntax;
            if (mre == null)
                return false;
            if (mre.Name.Identifier.ValueText != "LastIndexOf")
                return false;

            var rr = nodeContext.SemanticModel.GetSymbolInfo(node, nodeContext.CancellationToken);
            if (rr.Symbol == null)
                return false;
            var symbol = rr.Symbol;
            if (!(symbol.ContainingType != null && symbol.ContainingType.SpecialType == SpecialType.System_String))
                return false;
            var parameters = symbol.GetParameters();
            var firstParameter = parameters.FirstOrDefault();
            if (firstParameter == null || firstParameter.Type.SpecialType != SpecialType.System_String)
                return false;   // First parameter not a string
            var lastParameter = parameters.Last();
            if (lastParameter.Type.Name == "StringComparison")
                return false;   // already specifying a string comparison
            diagnostic = Diagnostic.Create(
                descriptor,
                node.GetLocation()
            );
            return true;
        }
    }
}
namespace EventSourceAnalyzers
{
    using System.Collections.Immutable;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider( LanguageNames.CSharp )]
    public class UseSameEventIdCodeFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create( DiagnosticIds.CallToWriteEventMustUseSameEventId );

        public override async Task RegisterCodeFixesAsync( CodeFixContext context )
        {
            var semanticModel = await context.Document.GetSemanticModelAsync( context.CancellationToken );
            var eventAttributeSymbol = EventSourceTypeSymbols.GetEventAttribute( semanticModel.Compilation );
            if ( eventAttributeSymbol == null )
            {
                return;
            }

            var root = await context.Document.GetSyntaxRootAsync( context.CancellationToken );
            var declaration = root.FindNode( context.Span )?.FirstAncestorOrSelf<ArgumentListSyntax>();

            if ( declaration == null ) return;

            var decl = declaration.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            foreach ( var attribList in decl.AttributeLists )
            {
                foreach ( var attrib in attribList.Attributes )
                {
                    // Check so that the event has event ID specified
                    if ( attrib.ArgumentList.Arguments.Count == 0 )
                        continue;

                    var attributeSymbolInfo = semanticModel.GetSymbolInfo( attrib ).Symbol;
                    // Check if the attribute is of EventAttribute type
                    if ( attributeSymbolInfo.ContainingType == eventAttributeSymbol )
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                "Use same event ID as event attribute",
                                ctx => ReplaceArgumentList( context.Document, root, declaration, attrib ) ),
                            context.Diagnostics );
                    }
                }
            }
        }

        public static Task<Document> ReplaceArgumentList( Document document, SyntaxNode root, ArgumentListSyntax decl, AttributeSyntax attrib )
        {
            var newArg = SyntaxFactory.Argument( attrib.ArgumentList.Arguments[0].Expression );
            SyntaxNode newRoot;

            // Add new argument if none there.
            if ( decl.Arguments.Count == 0 )
            {
                decl.Arguments.Add( newArg );
                newRoot = root.ReplaceNode( decl, decl );
            }
            // Replace existing first argument
            else
            {
                var newDec = decl.Arguments[0].WithExpression( newArg.Expression );
                newRoot = root.ReplaceNode( decl.Arguments[0], newDec );
            }

            return Task.FromResult( document.WithSyntaxRoot( newRoot ) );
        }
    }
}

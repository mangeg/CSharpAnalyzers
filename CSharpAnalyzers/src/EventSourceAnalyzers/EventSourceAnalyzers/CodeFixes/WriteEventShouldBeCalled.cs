namespace EventSourceAnalyzers
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider( LanguageNames.CSharp )]
    public class WriteEventShouldBeCalled : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create( DiagnosticIds.NoCallToWriteEvent );
        public override async Task RegisterCodeFixesAsync( CodeFixContext context )
        {
            var semanticModel = await context.Document.GetSemanticModelAsync( context.CancellationToken );
            var eventAttributeSymbol = EventSourceTypeSymbols.GetEventAttribute( semanticModel.Compilation );
            if ( eventAttributeSymbol == null )
            {
                return;
            }

            var root = await context.Document.GetSyntaxRootAsync( context.CancellationToken );
            var declaration = root.FindNode( context.Span )?.FirstAncestorOrSelf<BlockSyntax>();

            if ( declaration == null )
                return;

            var decl = declaration.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            foreach ( var attribList in decl.AttributeLists )
            {
                foreach ( var attrib in attribList.Attributes )
                {
                    if ( attrib.ArgumentList.Arguments.Count == 0 )
                        continue;

                    var attribSymbol = semanticModel.GetSymbolInfo( attrib ).Symbol;

                    if ( attribSymbol.ContainingType != eventAttributeSymbol ) continue;

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Use same event ID",
                            ctx => Document( context.Document, root, declaration, attrib ) ),
                        context.Diagnostics );

                    return;
                }
            }
        }
        public static Task<Document> Document( Document document, SyntaxNode root, BlockSyntax declaration, AttributeSyntax attrib )
        {
            var newStatements = new List<StatementSyntax>();
            newStatements.AddRange( declaration.Statements );

            var methodSyntax = declaration.FirstAncestorOrSelf<MethodDeclarationSyntax>();

            if ( methodSyntax == null )
                return Task.FromResult( document );

            var args = new SeparatedSyntaxList<ArgumentSyntax>();

            args = args.Add( SyntaxFactory.Argument( attrib.ArgumentList.Arguments[0].Expression ) );

            foreach ( var parementer in methodSyntax.ParameterList.Parameters )
            {
                var identifier = SyntaxFactory.IdentifierName( parementer.Identifier.ValueText );
                var arg = SyntaxFactory.Argument( identifier );
                args = args.Add( arg );
            }

            var invocationStatement =
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression( 
                        SyntaxFactory.IdentifierName( "WriteEvent" ), 
                        SyntaxFactory.ArgumentList( args ) ) );

            newStatements.Add( invocationStatement );
            var newBlock = SyntaxFactory.Block( newStatements );
            var newRoot = root.ReplaceNode( declaration, newBlock );

            return Task.FromResult( document.WithSyntaxRoot( newRoot ) );
        }
    }
}

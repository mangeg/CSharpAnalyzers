namespace EventSourceAnalyzers.CodeFixes
{
    using System.Collections.Immutable;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider( LanguageNames.CSharp )]
    public class ParametersNotPassedInTheSameOrderFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create( DiagnosticIds.ParametersNotPassedInTheSameOrder, DiagnosticIds.NotAllInputParametersPassed );

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

            if ( declaration == null )
                return;

            if ( declaration.Arguments.Count == 0 )
                return;

            var decl = declaration.FirstAncestorOrSelf<MethodDeclarationSyntax>();

            if ( decl == null )
                return;

            if ( decl?.ParameterList.Parameters.Count == 0 )
                return;

            var sameOrderDiag = context.Diagnostics.Where( d => d.Id == DiagnosticIds.ParametersNotPassedInTheSameOrder ).ToList();
            var allParamsDiag = context.Diagnostics.Where( d => d.Id == DiagnosticIds.NotAllInputParametersPassed ).ToList();

            if ( sameOrderDiag.Any() )
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Pass paramters in the same order as passed to the event.",
                        ctx => ReplaceParameterList( context, declaration, decl, root ) ),
                    sameOrderDiag );

            if ( allParamsDiag.Any() )
                context.RegisterCodeFix(
                    CodeAction.Create(
                        "Add all arguments to paramters to WriteEvent",
                        ctx => ReplaceParameterList( context, declaration, decl, root ) ),
                    allParamsDiag );
        }

        private static Task<Document> ReplaceParameterList(
            CodeFixContext context,
            BaseArgumentListSyntax declaration,
            BaseMethodDeclarationSyntax decl,
            SyntaxNode root )
        {
            var args = new SeparatedSyntaxList<ArgumentSyntax>();
            args = args.Add( SyntaxFactory.Argument( declaration.Arguments[0].Expression ) );
            foreach ( var param in decl.ParameterList.Parameters )
            {
                var identifier = SyntaxFactory.IdentifierName( param.Identifier.ValueText );
                var arg = SyntaxFactory.Argument( identifier );
                args = args.Add( arg );
            }

            var newArgList = SyntaxFactory.ArgumentList( args );

            var newRoot = root.ReplaceNode( declaration, newArgList );

            return Task.FromResult( context.Document.WithSyntaxRoot( newRoot ) );
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}

namespace EventSourceAnalyzers.CodeFixes
{
    using System.Collections.Immutable;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    [ExportCodeFixProvider( LanguageNames.CSharp )]
    public class MethodShouldHaveAttributeFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create<string>( DiagnosticIds.MethodShouldHaveAttributes );

        public override async Task RegisterCodeFixesAsync( CodeFixContext context )
        {
            var semanticModel = await context.Document.GetSemanticModelAsync( context.CancellationToken );
            var eventAttributeSymbol = EventSourceTypeSymbols.GetEventAttribute( semanticModel.Compilation );
            if ( eventAttributeSymbol == null )
            {
                return;
            }

            var root = await context.Document.GetSyntaxRootAsync( context.CancellationToken );
            var declaration = root.FindNode( context.Span )?.FirstAncestorOrSelf<MethodDeclarationSyntax>();

            if ( declaration == null )
                return;

            context.RegisterCodeFix( CodeAction.Create( "Add Event attribute",
                ctx => {
                    return Task.FromResult( context.Document );
                } ),
                context.Diagnostics );
            context.RegisterCodeFix( CodeAction.Create( "Add NonEvent attribute",
                ctx => {
                    /*var attribs = new SeparatedSyntaxList<AttributeSyntax>();
                    attribs = attribs.Add( SyntaxFactory.Attribute( SyntaxFactory.IdentifierName( "NonEvent" ) ) );

                    declaration.AttributeLists.Add( SyntaxFactory.AttributeList( attribs ) );

                    var newRoot = root.InsertNodesBefore( declaration, new[] { SyntaxFactory.AttributeList( attribs ) } );

                    return Task.FromResult( context.Document.WithSyntaxRoot( newRoot ) );*/
                    return Task.FromResult( context.Document );
                } ),
                context.Diagnostics );
        }
    }
}
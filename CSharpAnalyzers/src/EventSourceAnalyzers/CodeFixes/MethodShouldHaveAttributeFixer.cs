namespace EventSourceAnalyzers.CodeFixes
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
    public class MethodShouldHaveAttributeFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create( DiagnosticIds.MethodShouldHaveAttributes );

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

            var classDecl = declaration.FirstAncestorOrSelf<ClassDeclarationSyntax>();

            if ( classDecl == null )
                return;

            var classSymbol = semanticModel.GetDeclaredSymbol( classDecl );

            if ( classSymbol == null )
                return;

            var allConstants = classSymbol.GetMembers().OfType<IFieldSymbol>().Where( f => f.ConstantValue is int );

            var currentValues = classSymbol.GetUsedEventIds( semanticModel ).ToList();

            foreach ( var constant in allConstants )
            {
                var constantValue = (int)constant.ConstantValue;
                var eventId = constantValue + 1;
                while ( currentValues.Contains( eventId ) )
                    eventId++;

                eventId = eventId - constantValue;

                context.RegisterCodeFix(
                CodeAction.Create(
                    $"Add Event attribute ({constant.Name} - {constantValue + eventId})", 
                    ctx =>
                    {
                        return AddAttribute( context, constant.Name, eventId, declaration, semanticModel, root );
                    } ),
                context.Diagnostics );
            }

            

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Add NonEvent attribute",
                    ctx =>
                    {
                        var attribs = SyntaxFactory.SeparatedList( new[] { SyntaxFactory.Attribute( SyntaxFactory.IdentifierName( "NonEvent" ) ) } );

                        var newMethodDecl = SyntaxFactory.MethodDeclaration(
                            SyntaxFactory.List( new[] { SyntaxFactory.AttributeList( attribs ) } ),
                            declaration.Modifiers,
                            declaration.ReturnType,
                            declaration.ExplicitInterfaceSpecifier,
                            declaration.Identifier,
                            declaration.TypeParameterList,
                            declaration.ParameterList,
                            declaration.ConstraintClauses,
                            declaration.Body,
                            declaration.SemicolonToken );

                        var newRoot = root.ReplaceNode( declaration, newMethodDecl );

                        return Task.FromResult( context.Document.WithSyntaxRoot( newRoot ) );
                    } ),
                context.Diagnostics );
        }

        private static Task<Document> AddAttribute(
            CodeFixContext context,
            string constantName,
            int eventId,
            MethodDeclarationSyntax method,
            SemanticModel semanticModel,
            SyntaxNode root )
        {
            var eventSourceSymbol = EventSourceTypeSymbols.GetEventSource( semanticModel.Compilation );

            var replacementInfo = new Dictionary<SyntaxNode, SyntaxNode>();
            var newParam = SyntaxFactory.AttributeArgument(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.AddExpression,
                    SyntaxFactory.IdentifierName( constantName ),
                    SyntaxFactory.LiteralExpression( SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal( eventId ) )
                    )
                );

            var attribs = SyntaxFactory.SeparatedList(
                new[] {
                    SyntaxFactory.Attribute(
                        SyntaxFactory.IdentifierName( "Event" ),
                        SyntaxFactory.AttributeArgumentList(
                            SyntaxFactory.SeparatedList(
                                new[] { newParam } ) ) )
                } );

            var newMethodDecl = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.List( new[] { SyntaxFactory.AttributeList( attribs ) } ),
                method.Modifiers,
                method.ReturnType,
                method.ExplicitInterfaceSpecifier,
                method.Identifier,
                method.TypeParameterList,
                method.ParameterList,
                method.ConstraintClauses,
                method.Body,
                method.SemicolonToken );

            var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach ( var invocationExpressionSyntax in invocations )
            {
                var invocationSymbol =
                    semanticModel.GetSymbolInfo( invocationExpressionSyntax ).Symbol as IMethodSymbol;

                if ( invocationSymbol == null )
                    continue;

                if ( !Equals( eventSourceSymbol, invocationSymbol.ContainingSymbol ) )
                    continue;

                if ( invocationSymbol.Name == "WriteEvent" )
                {
                    replacementInfo.Add(
                        invocationExpressionSyntax.ArgumentList.Arguments.First(),
                        SyntaxFactory.Argument( newParam.Expression ) );
                }
            }

            replacementInfo.Add( method, newMethodDecl );

            var newRoot = root.ReplaceNodes(
                replacementInfo.Keys,
                ( n1, n2 ) => replacementInfo[n1] );

            return Task.FromResult( context.Document.WithSyntaxRoot( newRoot ) );
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}

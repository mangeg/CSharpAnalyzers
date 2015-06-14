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
    public class EventIdUsedFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create( DiagnosticIds.EventNumberUsedMultipleTimes );

        public override async Task RegisterCodeFixesAsync( CodeFixContext context )
        {
            var semanticModel = await context.Document.GetSemanticModelAsync( context.CancellationToken );

            var eventAttributeSymbol = EventSourceTypeSymbols.GetEventAttribute( semanticModel.Compilation );
            var eventSourceSymbol = EventSourceTypeSymbols.GetEventSource( semanticModel.Compilation );

            if ( eventAttributeSymbol == null || eventSourceSymbol == null )
            {
                return;
            }

            var root = await context.Document.GetSyntaxRootAsync( context.CancellationToken );
            var declaration = root.FindNode( context.Span )?.FirstAncestorOrSelf<MethodDeclarationSyntax>();

            if ( declaration == null )
                return;

            var methodSymbol = semanticModel.GetDeclaredSymbol( declaration );

            if ( methodSymbol == null )
                return;

            if ( !methodSymbol.ContainingType.IsDerivedFrom( eventSourceSymbol ) )
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
                        $"Use next free number under '{constant.Name} ({constantValue}) - {constantValue + eventId}'",
                        ctx => ChangeEventId( context.Document, root, semanticModel, constant.Name, eventId, declaration ) ),
                    context.Diagnostics );
            }
        }

        internal static Task<Document> ChangeEventId(
            Document document,
            SyntaxNode root,
            SemanticModel semanticModel,
            string constantName,
            int eventId,
            MethodDeclarationSyntax method)
        {
            var eventAttributeSymbol = EventSourceTypeSymbols.GetEventAttribute( semanticModel.Compilation );
            var eventSourceSymbol = EventSourceTypeSymbols.GetEventSource( semanticModel.Compilation );

            var replacementInfo = new Dictionary<SyntaxNode, SyntaxNode>();

            var newParam = SyntaxFactory.AttributeArgument(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.AddExpression,
                    SyntaxFactory.IdentifierName( constantName ),
                    SyntaxFactory.LiteralExpression( SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal( eventId ) )
                    )
                );

            foreach ( var attributeList in method.AttributeLists )
            {
                foreach ( var attrib in attributeList.Attributes )
                {
                    var attribSymbol = semanticModel.GetSymbolInfo( attrib ).Symbol as IMethodSymbol;
                    if ( attribSymbol == null )
                        continue;

                    if ( attribSymbol.ContainingType == eventAttributeSymbol )
                    {
                        replacementInfo.Add( attrib.ArgumentList.Arguments.First(), newParam );
                    }
                }
            }

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

            var newRoot = root.ReplaceNodes(
                replacementInfo.Keys,
                ( n1, n2 ) => replacementInfo[n1] );

            return Task.FromResult( document.WithSyntaxRoot( newRoot ) );
        }
    }
}

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
    public class UseConstantAdderFixer : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create( DiagnosticIds.UseConstantAddersForEventId );

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
            var decl = root.FindNode( context.Span )?.FirstAncestorOrSelf<AttributeArgumentListSyntax>();
            var classDecl = decl?.FirstAncestorOrSelf<ClassDeclarationSyntax>();

            if ( decl == null || classDecl == null ) return;

            if ( !decl.Arguments.Any() )
                return;

            var fields = classDecl.Members.OfType<FieldDeclarationSyntax>();
            foreach ( var fieldDecl in fields )
            {
                if ( fieldDecl.Modifiers.Any( m => m.IsKind( SyntaxKind.ConstKeyword ) ) )
                {
                    var typeSymbol = semanticModel.GetSymbolInfo( fieldDecl.Declaration.Type ).Symbol as INamedTypeSymbol;
                    if ( typeSymbol == null ) continue;

                    if ( typeSymbol.SpecialType == SpecialType.System_Int16 || typeSymbol.SpecialType == SpecialType.System_Int32 ||
                        typeSymbol.SpecialType == SpecialType.System_Int64 || typeSymbol.SpecialType == SpecialType.System_UInt16 ||
                        typeSymbol.SpecialType == SpecialType.System_UInt32 ||
                        typeSymbol.SpecialType == SpecialType.System_UInt64 && fieldDecl.Declaration.Variables.Any() )
                    {
                        var identifier = fieldDecl.Declaration.Variables.First().Identifier.ValueText;

                        var fixText = $"Add variable '{identifier}' to event id paramter";

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                fixText,
                                ctx => AddConstantVariable( context, decl, identifier, semanticModel, eventSourceSymbol, root ) ),
                            context.Diagnostics );
                    }
                }
            }
        }
        private static Task<Document> AddConstantVariable(
            CodeFixContext context,
            AttributeArgumentListSyntax decl,
            string identifier,
            SemanticModel semanticModel,
            ITypeSymbol eventSourceSymbol,
            SyntaxNode root )
        {
            var replacementInfo = new Dictionary<SyntaxNode, SyntaxNode>();
            var original = decl.Arguments.First();

            var newAttributeArgument = SyntaxFactory.AttributeArgument(
                SyntaxFactory.BinaryExpression(
                    SyntaxKind.AddExpression,
                    SyntaxFactory.IdentifierName( identifier ),
                    original.Expression ) );

            replacementInfo.Add( original, newAttributeArgument );

            var methodDecl = decl.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>();

            var allInvocations =
                methodDecl.Body.Statements.OfType<ExpressionStatementSyntax>()
                    .Where( s => s.Expression is InvocationExpressionSyntax )
                    .Select( s => s.Expression as InvocationExpressionSyntax );

            var writeEventMethodSymbols = new List<InvocationExpressionSyntax>();
            foreach ( var invocationExpressionSyntax in allInvocations )
            {
                var invocationSymbol =
                    semanticModel.GetSymbolInfo( invocationExpressionSyntax ).Symbol as IMethodSymbol;
                if ( invocationSymbol == null )
                    continue;

                if ( !Equals( eventSourceSymbol, invocationSymbol.ContainingSymbol ) )
                    continue;

                if ( invocationSymbol.Name == "WriteEvent" )
                    writeEventMethodSymbols.Add( invocationExpressionSyntax );
            }

            foreach ( var writeEventMethodSymbol in writeEventMethodSymbols )
            {
                var eventIdArg = writeEventMethodSymbol.ArgumentList.Arguments.First();
                replacementInfo.Add( eventIdArg, SyntaxFactory.Argument( newAttributeArgument.Expression ) );
            }


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
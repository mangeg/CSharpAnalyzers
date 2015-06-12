namespace EventSourceAnalyzers.CodeFixes
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.Tracing;
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
                            "Add WriteEvent call",
                            ctx => AddWriteEventMethodSimple( context.Document, root, declaration, attrib, 1 ) ),
                        context.Diagnostics );
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Add WriteEvent call with enabled check",
                            ctx => AddWriteEventMethodSimple( context.Document, root, declaration, attrib, 2 ) ),
                        context.Diagnostics );
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Add WriteEvent call with enabled check. (Detailed)",
                            ctx => AddWriteEventMethodSimple( context.Document, root, declaration, attrib, 3 ) ),
                        context.Diagnostics );

                    return;
                }
            }
        }

        public static Task<Document> AddWriteEventMethodSimple(
            Document document,
            SyntaxNode root,
            BlockSyntax declaration,
            AttributeSyntax attrib,
            int detailLevel = 1 )
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

            if ( detailLevel == 1 )
            {
                var writeEventStatement = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName( "WriteEvent" ),
                        SyntaxFactory.ArgumentList( args ) ) );
                newStatements.Add( writeEventStatement );
            }
            else if ( detailLevel == 2 )
            {
                var writeEventStatement = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName( "WriteEvent" ),
                        SyntaxFactory.ArgumentList( args ) ) );

                var enabledCheckStatement =
                    SyntaxFactory.IfStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.IdentifierName( "IsEnabled" ) ),
                        writeEventStatement );
                newStatements.Add( enabledCheckStatement );
            }
            else
            {
                var levelAttrib =
                    attrib.ArgumentList.Arguments.FirstOrDefault( a => a.NameEquals?.Name?.Identifier.ValueText == "Level" );
                var keywordsAttrib =
                    attrib.ArgumentList.Arguments.FirstOrDefault( a => a.NameEquals?.Name?.Identifier.ValueText == "Keywords" );

                

                var enabledCheckArgs = new SeparatedSyntaxList<ArgumentSyntax>();

                if ( levelAttrib != null )
                    enabledCheckArgs = enabledCheckArgs.Add( SyntaxFactory.Argument( levelAttrib.Expression ) );
                else
                {
                    enabledCheckArgs =
                        enabledCheckArgs.Add(
                            SyntaxFactory.Argument(
                                SyntaxFactory.MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName( "EventLevel" ),
                                    SyntaxFactory.IdentifierName( "LogAlways" ) ) ) );
                }
                if ( keywordsAttrib != null )
                    enabledCheckArgs = enabledCheckArgs.Add( SyntaxFactory.Argument( keywordsAttrib.Expression ) );
                else
                {
                    enabledCheckArgs =
                        enabledCheckArgs.Add(
                            SyntaxFactory.Argument(
                                SyntaxFactory.MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression,
                                    SyntaxFactory.IdentifierName( "EventKeywords" ),
                                    SyntaxFactory.IdentifierName( "None" ) ) ) );
                }

                var writeEventStatement = SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.IdentifierName( "WriteEvent" ),
                        SyntaxFactory.ArgumentList( args ) ) );
                var enabledCheckStatement =
                    SyntaxFactory.IfStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.IdentifierName( "IsEnabled" ),
                            SyntaxFactory.ArgumentList( enabledCheckArgs ) ),
                        writeEventStatement );

                newStatements.Add( enabledCheckStatement );
            }

            
            var newBlock = SyntaxFactory.Block( newStatements );
            var newRoot = root.ReplaceNode( declaration, newBlock );

            return Task.FromResult( document.WithSyntaxRoot( newRoot ) );
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }
    }

    class TestEvent : EventSource
    {
        [Event( 5, Message = "Hello", Level = EventLevel.LogAlways, Keywords = EventKeywords.None )]
        public void EventOne( string iuput1 )
        {
            if ( IsEnabled() )
                WriteEvent( 3, iuput1 );
            if ( IsEnabled( EventLevel.Error, EventKeywords.None ) )
                WriteEvent( 5, iuput1 );
        }

        [NonEvent]
        public void EventTwo()
        {
        }
    }
}

namespace EventSourceAnalyzers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics.Tracing;
    using System.Linq;
    using System.Threading;
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
            var root = await context.Document.GetSyntaxRootAsync( context.CancellationToken );
            var declaration = root.FindNode( context.Span )?.FirstAncestorOrSelf<ArgumentListSyntax>();

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use same event ID",
                    token => Document( context.Document, root, declaration, token ) ),
                context.Diagnostics );
        }

        public static async Task<Document> Document(
            Document document,
            SyntaxNode root,
            ArgumentListSyntax declaration,
            CancellationToken ctx )
        {
            try
            {
                var semanticModel = await document.GetSemanticModelAsync( ctx );
                var eventAttributeTypeName =
                    semanticModel.Compilation.GetTypeByMetadataName( EventSourceTypeNames.EventAttribute );
                var decl = declaration.FirstAncestorOrSelf<MethodDeclarationSyntax>();
                foreach ( var attributeListSyntax in decl.AttributeLists )
                {
                    foreach ( var attributeSyntax in attributeListSyntax.Attributes )
                    {
                        var attributeSymbolInfo = semanticModel.GetSymbolInfo( attributeSyntax ).Symbol;
                        if ( attributeSymbolInfo.ContainingType == eventAttributeTypeName )
                        {
                            var newDec =
                                declaration.Arguments[0].WithExpression(
                                    attributeSyntax.ArgumentList.Arguments[0].Expression );
                            var newRoot = root.ReplaceNode( declaration.Arguments[0], newDec );
                            return document.WithSyntaxRoot( newRoot );
                        }
                    }
                }
            }
            catch
            {
                // ignored
            }

            return document;
        }
    }

    [ExportCodeFixProvider( LanguageNames.CSharp )]
    public class WriteEventShouldBeCalled : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create( DiagnosticIds.NoCallToWriteEvent );

        public override async Task RegisterCodeFixesAsync( CodeFixContext context )
        {
            var root = await context.Document.GetSyntaxRootAsync( context.CancellationToken );
            var declaration = root.FindNode( context.Span )?.FirstAncestorOrSelf<BlockSyntax>();

            context.RegisterCodeFix(
                CodeAction.Create(
                    "Use same event ID",
                    token => Document( context.Document, root, declaration, token ) ),
                context.Diagnostics );
        }

        public static async Task<Document> Document(
            Document document,
            SyntaxNode root,
            BlockSyntax declaration,
            CancellationToken ctx )
        {
            try
            {
                var semanticModel = await document.GetSemanticModelAsync( ctx );
                var eventAttributeTypeName =
                    semanticModel.Compilation.GetTypeByMetadataName( EventSourceTypeNames.EventAttribute );
                var decl = declaration.FirstAncestorOrSelf<MethodDeclarationSyntax>();
                foreach ( var attributeListSyntax in decl.AttributeLists )
                {
                    foreach ( var attributeSyntax in attributeListSyntax.Attributes )
                    {
                        var attributeSymbolInfo = semanticModel.GetSymbolInfo( attributeSyntax ).Symbol;
                        if ( attributeSymbolInfo.ContainingType == eventAttributeTypeName )
                        {
                            var newStateMets = new List<StatementSyntax>();
                            newStateMets.AddRange( declaration.Statements );

                            if ( newStateMets.Any( s => s is ReturnStatementSyntax ) )
                            {
                                
                            }

                            var methodSyntax = declaration.FirstAncestorOrSelf<MethodDeclarationSyntax>();

                            var args = new SeparatedSyntaxList<ArgumentSyntax>();
                            var firstArg = SyntaxFactory.Argument(
                                attributeSyntax.ArgumentList.Arguments[0].Expression );
                            args = args.Add( firstArg );
                            foreach ( var parementer in methodSyntax.ParameterList.Parameters )
                            {
                                var identifier = SyntaxFactory.IdentifierName( parementer.Identifier.ValueText );
                                var arg = SyntaxFactory.Argument( identifier );
                                args = args.Add( arg );
                            }
                            var argList = SyntaxFactory.ArgumentList( args );
                            var invocationExpression =
                                SyntaxFactory.InvocationExpression( SyntaxFactory.IdentifierName( "WriteEvent" ), argList );
                            var invocationStatement = SyntaxFactory.ExpressionStatement(invocationExpression );

                            newStateMets.Add( invocationStatement );
                            var newBlock = SyntaxFactory.Block( newStateMets.ToArray() );
                            var newRoot = root.ReplaceNode( declaration, newBlock );
                            return document.WithSyntaxRoot( newRoot );
                        }
                    
                    }
                }
            }
            catch (Exception e)
            {
                // ignored
            }

            return document;
        }
    }

    public class Test : EventSource
    {
        public void TestEvent(string arg1, string arg2)
        {
            WriteEvent( 1, arg1 );
        }
    }
}

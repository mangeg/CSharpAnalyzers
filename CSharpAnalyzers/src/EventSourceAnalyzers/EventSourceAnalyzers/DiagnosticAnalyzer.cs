namespace EventSourceAnalyzers
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer( LanguageNames.CSharp )]
    public class EventSourceAnalyzersAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(
                    DiagnosticDescriptors.EventNumberUsedMultipleTimes,
                    DiagnosticDescriptors.UseConstantAddersForEventId,
                    DiagnosticDescriptors.CallToWriteEventMustUseSameEventId,
                    DiagnosticDescriptors.CallToWriteEventIdShouldBeConstant );
            }
        }
        public override void Initialize( AnalysisContext context )
        {
            context.RegisterSymbolAction( CheckForDuplicateEventIds, SymbolKind.NamedType );
            context.RegisterSyntaxNodeAction( CheckForVariableInEventParam, SyntaxKind.AttributeArgument );
            context.RegisterSyntaxNodeAction( CheckWriteEventIdToEventAttributeId, SyntaxKind.InvocationExpression );
        }

        internal static void CheckWriteEventIdToEventAttributeId( SyntaxNodeAnalysisContext ctx )
        {
            var invoke = ctx.Node as InvocationExpressionSyntax;
            if ( invoke == null )
                return;

            var invokationModel = ctx.SemanticModel.GetSymbolInfo( invoke );

            var invokeMethodSymbol = invokationModel.Symbol as IMethodSymbol;
            if ( invokeMethodSymbol == null )
                return;

            var eventSourceType = ctx.SemanticModel.Compilation.GetTypeByMetadataName( EventSourceTypeNames.EventSource );
            if ( !Equals( eventSourceType, invokeMethodSymbol.ContainingSymbol ) )
            {
                return;
            }

            if ( invokeMethodSymbol.Name != "WriteEvent" )
                return;

            if ( invoke.ArgumentList.Arguments.Count < 1 )
                return;

            var argExpression = invoke.ArgumentList.Arguments.First().Expression;
            var constantValue = ctx.SemanticModel.GetConstantValue( argExpression );
            if ( !constantValue.HasValue )
            {
                ctx.ReportDiagnostic( Diagnostic.Create( DiagnosticDescriptors.CallToWriteEventIdShouldBeConstant,
                    argExpression.GetLocation() ) );
                return;
            }

            if ( !( constantValue.Value is int ) )
                return;

            var methodDecl = invoke.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if ( methodDecl == null )
                return;

            var methodInfo = ctx.SemanticModel.GetDeclaredSymbol( methodDecl );


            if ( methodInfo == null )
                return;

            int eventId;
            if ( methodInfo.TryGetEventId( out eventId, ctx.SemanticModel.Compilation ) )
            {
                var invokeId = (int)constantValue.Value;
                if ( eventId != invokeId )
                {
                    ctx.ReportDiagnostic( Diagnostic.Create( DiagnosticDescriptors.CallToWriteEventMustUseSameEventId,
                        argExpression.GetLocation(),
                        invokeId,
                        eventId ) );
                }
            }
        }
        internal static void CheckForVariableInEventParam( SyntaxNodeAnalysisContext ctx )
        {
            var argument = (AttributeArgumentSyntax)ctx.Node;

            if ( !ctx.SemanticModel.IsAttributeType( argument, EventSourceTypeNames.EventAttribute ) )
                return;

            if ( argument.GetArgumentPosition() != 0 )
                return;

            var expression = argument.Expression;

            bool doShow = expression?.IsKind( SyntaxKind.NumericLiteralExpression ) == true;
            if ( expression is BinaryExpressionSyntax )
            {
                var binaryExpression = expression as BinaryExpressionSyntax;
                doShow = !binaryExpression.ContainsVariable();
            }

            if ( doShow )
            {
                var location = argument.GetLocation();
                ctx.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.UseConstantAddersForEventId,
                        location ) );
            }
        }
        internal static void CheckForDuplicateEventIds( SymbolAnalysisContext ctx )
        {
            if ( ctx.CancellationToken.IsCancellationRequested )
                return;

            if ( ctx.Symbol.GetAttributeOfType( EventSourceTypeNames.EventSourceAttribute, ctx.Compilation ) == null )
                return;

            var nameType = (INamedTypeSymbol)ctx.Symbol;
            var methodIds = new Dictionary<int, List<IMethodSymbol>>();
            var allMethods = nameType.GetMembers().OfType<IMethodSymbol>().Where( m => m.MethodKind == MethodKind.Ordinary );
            foreach ( var method in allMethods )
            {
                int eventId;
                if ( method.TryGetEventId( out eventId, ctx.Compilation ) )
                {
                    if ( !methodIds.ContainsKey( eventId ) )
                    {
                        methodIds.Add( eventId, new List<IMethodSymbol>() );
                    }
                    methodIds[eventId].Add( method );
                }
            }

            foreach ( var methods in methodIds.Where( m => m.Value.Count > 1 ) )
            {
                foreach ( var method in methods.Value )
                {
                    var names = string.Join(
                        ", ",
                        methods.Value.Where( m => !Equals( m, method ) ).Select( m => m.Name ) );

                    ctx.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.EventNumberUsedMultipleTimes,
                            method.Locations[0],
                            methods.Key,
                            names ) );
                }
            }
        }
    }
}

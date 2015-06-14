namespace EventSourceAnalyzers.Analyzers
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;
    using Microsoft.CodeAnalysis.Text;

    [DiagnosticAnalyzer( LanguageNames.CSharp )]
    internal class EventSourceAnalyzers : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
            DiagnosticDescriptors.EventNumberUsedMultipleTimes,
            DiagnosticDescriptors.UseConstantAddersForEventId,
            DiagnosticDescriptors.CallToWriteEventMustUseSameEventId,
            DiagnosticDescriptors.CallToWriteEventIdShouldBeConstant,
            DiagnosticDescriptors.NoCalToWriteEvent,
            DiagnosticDescriptors.MultipleCallToWriteEvent,
            DiagnosticDescriptors.ParametersNotPassedInTheSameOrder,
            DiagnosticDescriptors.NotAllInputParametersPassed,
            DiagnosticDescriptors.MethodsShouldHaveAttributes );

        public override void Initialize( AnalysisContext context )
        {
            context.RegisterSymbolAction( CheckForDuplicateEventIds, SymbolKind.NamedType );
            context.RegisterSymbolAction( CheckMethodsHaveAttribute, SymbolKind.Method );
            context.RegisterSyntaxNodeAction( CheckForVariableInEventParam, SyntaxKind.AttributeArgument );
            context.RegisterSyntaxNodeAction( CheckWriteEventIdToEventAttributeId, SyntaxKind.InvocationExpression );
            context.RegisterSyntaxNodeAction( CheckWriteEventCall, SyntaxKind.ParameterList );
        }

        internal static void CheckMethodsHaveAttribute( SymbolAnalysisContext ctx )
        {
            var methodSymbol = ctx.Symbol as IMethodSymbol;
            if ( methodSymbol == null ) return;

            var eventSourceSymbol = EventSourceTypeSymbols.GetEventSource( ctx.Compilation );
            var eventAttributeSymbol = EventSourceTypeSymbols.GetEventAttribute( ctx.Compilation );
            var nonEventAttributeSymbol = EventSourceTypeSymbols.GetNonEventAttribute( ctx.Compilation );

            if ( eventSourceSymbol == null || eventAttributeSymbol == null || nonEventAttributeSymbol == null )
                return;

            if ( !methodSymbol.ContainingType.IsDerivedFrom( eventSourceSymbol ) )
                return;

            var attribs = methodSymbol.GetAttributes();

            if ( !attribs.Any( a => Equals( a.AttributeClass, eventAttributeSymbol ) || Equals( a.AttributeClass, nonEventAttributeSymbol ) ) )
            {
                ctx.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MethodsShouldHaveAttributes,
                        methodSymbol.Locations[0] ) );
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
            var allMethods =
                nameType.GetMembers().OfType<IMethodSymbol>().Where( m => m.MethodKind == MethodKind.Ordinary );
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
                var names = string.Join(
                        ", ",
                        methods.Value.Where( m => !Equals( m, methods.Value.Last() ) ).Select( m => m.Name ) );
                ctx.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.EventNumberUsedMultipleTimes,
                        methods.Value.Last().Locations[0],
                        methods.Key,
                        names ) );
            }
        }
        internal static void CheckWriteEventCall( SyntaxNodeAnalysisContext ctx )
        {

            var eventSourceType = EventSourceTypeSymbols.GetEventSource( ctx.SemanticModel.Compilation );
            var eventAttributeType = EventSourceTypeSymbols.GetEventAttribute( ctx.SemanticModel.Compilation );
            if ( eventSourceType == null )
                return;

            var parameterListSyntax = ctx.Node as ParameterListSyntax;
            if ( parameterListSyntax == null ) return;

            var methodDeclSyntax = parameterListSyntax.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            // Parameter list but no method?
            if ( methodDeclSyntax == null )
                return;

            // No Event attribute
            if ( !methodDeclSyntax.AttributeLists.Any( l => l.Attributes.Any() ) )
                return;

            var found = false;
            foreach ( var attribList in methodDeclSyntax.AttributeLists )
            {
                foreach ( var attrib in attribList.Attributes )
                {
                    var attribSymbol = ctx.SemanticModel.GetSymbolInfo( attrib );
                    if ( attribSymbol.Symbol?.ContainingType == eventAttributeType )
                    {
                        found = true;
                    }
                }
            }

            if ( !found )
                return;

            var methodSymbolInfo = ctx.SemanticModel.GetDeclaredSymbol( methodDeclSyntax );

            if ( !Equals( eventSourceType, methodSymbolInfo.ContainingType.BaseType ) )
                return;

            // Typing new method
            if ( methodDeclSyntax.Body == null )
                return;

            var allInvocations = methodDeclSyntax.Body.DescendantNodes().OfType<InvocationExpressionSyntax>();

            var writeEventMethodSymbols = new List<InvocationExpressionSyntax>();
            foreach ( var invocationExpressionSyntax in allInvocations )
            {
                var invocationSymbol =
                    ctx.SemanticModel.GetSymbolInfo( invocationExpressionSyntax ).Symbol as IMethodSymbol;
                if ( invocationSymbol == null )
                    continue;

                if ( !Equals( eventSourceType, invocationSymbol.ContainingSymbol ) )
                    continue;

                if ( invocationSymbol.Name == "WriteEvent" )
                    writeEventMethodSymbols.Add( invocationExpressionSyntax );
            }

            if ( writeEventMethodSymbols.Count == 0 )
            {
                ctx.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.NoCalToWriteEvent,
                        methodDeclSyntax.Body.GetLocation() ) );
                return;
            }

            if ( writeEventMethodSymbols.Count > 1 )
            {
                ctx.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.MultipleCallToWriteEvent,
                        methodDeclSyntax.Body.GetLocation() ) );
            }

            foreach ( var writeEventMethodSymbol in writeEventMethodSymbols )
            {
                if ( writeEventMethodSymbol.ArgumentList.Arguments.Skip( 1 ).Count() !=
                    methodSymbolInfo.Parameters.Count() )
                {
                    var locations =
                        writeEventMethodSymbol.ArgumentList.Arguments.Skip( 1 )
                            .Select( a => a.GetLocation() )
                            .ToArray();

                    if ( locations.Any() )
                    {
                        var span = TextSpan.FromBounds(
                            locations.First().SourceSpan.Start,
                            locations.Last().SourceSpan.End );

                        var location = Location.Create( ctx.SemanticModel.SyntaxTree, span );
                        ctx.ReportDiagnostic(
                            Diagnostic.Create(
                                DiagnosticDescriptors.NotAllInputParametersPassed,
                                location ) );
                    }
                    else if( writeEventMethodSymbol.ArgumentList.Arguments.Any() )
                    {
                        ctx.ReportDiagnostic(
                            Diagnostic.Create(
                                DiagnosticDescriptors.NotAllInputParametersPassed,
                                writeEventMethodSymbol.ArgumentList.Arguments.First().GetLocation() ) );
                    }
                }

                for ( var i = 0; i < methodSymbolInfo.Parameters.Count(); i++ )
                {
                    if ( writeEventMethodSymbol.ArgumentList.Arguments.Skip( 1 ).Count() < i + 1 )
                        break;
                    var inputParameter = methodSymbolInfo.Parameters[i];
                    var invokeArgument = writeEventMethodSymbol.ArgumentList.Arguments[i + 1];

                    if ( inputParameter.Name !=
                        ( invokeArgument.Expression as IdentifierNameSyntax )?.Identifier.ValueText )
                    {
                        var locations =
                            writeEventMethodSymbol.ArgumentList.Arguments.Skip( 1 )
                                .Select( a => a.GetLocation() )
                                .ToArray();

                        var span = TextSpan.FromBounds(
                            locations.First().SourceSpan.Start,
                            locations.Last().SourceSpan.End );

                        var location = Location.Create( ctx.SemanticModel.SyntaxTree, span );

                        ctx.ReportDiagnostic(
                            Diagnostic.Create(
                                DiagnosticDescriptors.ParametersNotPassedInTheSameOrder,
                                location ) );
                        break;
                    }
                }
            }

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

            var eventSourceType = EventSourceTypeSymbols.GetEventSource( ctx.SemanticModel.Compilation );
            if ( !Equals( eventSourceType, invokeMethodSymbol.ContainingSymbol ) )
            {
                return;
            }

            if ( invokeMethodSymbol.Name != "WriteEvent" )
                return;

            var argExpression = invoke.ArgumentList.Arguments.First().Expression;
            var constantValue = ctx.SemanticModel.GetConstantValue( argExpression );
            if ( !constantValue.HasValue )
            {
                ctx.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.CallToWriteEventIdShouldBeConstant,
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
            if ( !methodInfo.TryGetEventId( out eventId, ctx.SemanticModel.Compilation ) )
                return;

            var invokeId = (int)constantValue.Value;
            if ( eventId != invokeId )
            {
                ctx.ReportDiagnostic(
                    Diagnostic.Create(
                        DiagnosticDescriptors.CallToWriteEventMustUseSameEventId,
                        argExpression.GetLocation(),
                        invokeId,
                        eventId ) );
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

            var doShow = expression?.IsKind( SyntaxKind.NumericLiteralExpression ) == true;
            if ( expression is BinaryExpressionSyntax )
            {
                var binaryExpression = expression as BinaryExpressionSyntax;
                doShow = !binaryExpression.ContainsVariable();
            }
            if ( expression is ParenthesizedExpressionSyntax )
            {
                doShow = ( expression as ParenthesizedExpressionSyntax ).ContainsVariable();
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
    }
}

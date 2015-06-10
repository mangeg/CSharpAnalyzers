namespace EventSourceAnalyzers
{
    using System;
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
        public const string DiagnosticId = "EventSourceAnalyzers";
        internal const string Category = "Naming";
        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return ImmutableArray.Create(
                    DiagnosticDescriptors.EventNumberUsedMultipleTimes,
                    DiagnosticDescriptors.UseConstantAddersForEventId );
            }
        }
        public override void Initialize( AnalysisContext context )
        {
            context.RegisterSymbolAction( CheckForDuplicateEventIds, SymbolKind.NamedType );
            context.RegisterSyntaxNodeAction( CheckForVariableInEventParam, SyntaxKind.AttributeArgument );
            
        }
        private static void CheckForVariableInEventParam( SyntaxNodeAnalysisContext context )
        {
                var argument = (AttributeArgumentSyntax)context.Node;
                    
                if ( !context.SemanticModel.IsAttributeType( argument, "System.Diagnostics.Tracing.EventAttribute" ) )
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

                if( doShow )
                {
                    var location = argument.GetLocation();
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.UseConstantAddersForEventId,
                            location ) );
                }
        }
        private static void CheckForDuplicateEventIds( SymbolAnalysisContext context )
        {
            if ( context.CancellationToken.IsCancellationRequested )
                return;

            if ( context.Symbol.GetAttributeOfType( "System.Diagnostics.Tracing.EventSourceAttribute", context ) == null )
                return;

            var nameType = (INamedTypeSymbol)context.Symbol;
            var methodIds = new Dictionary<int, List<IMethodSymbol>>();
            var allMethods = nameType.GetMembers().OfType<IMethodSymbol>().Where( m => m.MethodKind == MethodKind.Ordinary );
            foreach ( var method in allMethods )
            {
                var eventAttrib = method.GetAttributeOfType( "System.Diagnostics.Tracing.EventAttribute", context );
                if ( eventAttrib == null )
                    continue;
                
                if ( eventAttrib.ConstructorArguments.Any() && eventAttrib.ConstructorArguments.First().Kind == TypedConstantKind.Primitive )
                {
                    var argument = eventAttrib.ConstructorArguments.First();

                    var value = (int)argument.Value;
                    if ( !methodIds.ContainsKey( value ) )
                    {
                        methodIds.Add( value, new List<IMethodSymbol>() );
                    }
                    methodIds[value].Add( method );
                }
            }

            foreach ( var methods in methodIds.Where( m => m.Value.Count > 1 ) )
            {
                foreach ( var method in methods.Value )
                {
                    var names = string.Join(
                        ", ",
                        methods.Value.Where( m => !Equals( m, method ) ).Select( m => m.Name ) );
                    
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            DiagnosticDescriptors.EventNumberUsedMultipleTimes,
                            method.Locations[0],
                            methods.Key,
                            names ) );
                }
            }
        }
    }

    public static class Extensions
    {
        public static AttributeData GetAttributeOfType( this ISymbol method, string attributeTypeName, SymbolAnalysisContext context )
        {
            if ( attributeTypeName == null ) throw new ArgumentNullException( nameof( attributeTypeName ) );

            var attributeType = context.Compilation.GetTypeByMetadataName( attributeTypeName );
            if ( attributeType == null )
                return null;

            var attributeData = method.GetAttributes().FirstOrDefault( a => a.AttributeClass == attributeType );
            
            return attributeData;
        }

        public static BaseParameterListSyntax GetParameterList( this SyntaxNode node )
        {
            switch ( node?.Kind() )
            {
                case SyntaxKind.MethodDeclaration:
                    return ( node as MethodDeclarationSyntax )?.ParameterList;
                case SyntaxKind.ConstructorDeclaration:
                    return ( node as ConstructorDeclarationSyntax )?.ParameterList;
                case SyntaxKind.IndexerDeclaration:
                    return ( node as IndexerDeclarationSyntax )?.ParameterList;
                case SyntaxKind.ParenthesizedLambdaExpression:
                    return ( node as ParenthesizedLambdaExpressionSyntax )?.ParameterList;
                case SyntaxKind.AnonymousMethodExpression:
                    return ( node as AnonymousMethodExpressionSyntax )?.ParameterList;
                default:
                    return null;
            }
        }

        public static IEnumerable<ParameterSyntax> GetParametersInScope( this SyntaxNode node )
        {
            foreach ( var ancestor in node.AncestorsAndSelf() )
            {
                if ( ancestor.IsKind( SyntaxKind.SimpleLambdaExpression ) )
                {
                    yield return ( (SimpleLambdaExpressionSyntax)ancestor ).Parameter;
                }
                else
                {
                    var parameterList = ancestor.GetParameterList();
                    if ( parameterList != null )
                    {
                        foreach ( var parameter in parameterList.Parameters )
                        {
                            yield return parameter;
                        }
                    }
                }
            }
        }

        public static INamedTypeSymbol GetAttribute( this SemanticModel semanticModel, AttributeArgumentSyntax attribArgument  )
        {
            var argumentList = attribArgument.Parent as AttributeArgumentListSyntax;
            if ( argumentList == null )
                return null;

            var attributeSyntax = argumentList.Parent as AttributeSyntax;
            if ( attributeSyntax == null )
                return null;

            var attrib = semanticModel.GetSymbolInfo( attributeSyntax ).Symbol;
            if(attrib == null)
                return null;
            return attrib.ContainingType;
        }

        public static int GetArgumentPosition( this AttributeArgumentSyntax argument )
        {
         var argumentList = argument.Parent as AttributeArgumentListSyntax;
            if ( argumentList == null )
                throw new ArgumentException( "No parent argument list found", nameof( argument ) );

            return argumentList.Arguments.IndexOf( argument );
        }

        public static bool IsAttributeType(
            this SemanticModel semanticModel,
            AttributeArgumentSyntax attributeArgument, string attributeTypeName )
        {
            var attributeType = semanticModel.Compilation.GetTypeByMetadataName( attributeTypeName );
            if ( attributeType == null )
                return false;

            var attrib = semanticModel.GetAttribute( attributeArgument );

            if ( attrib == null )
                return false;

            return attrib.ConstructedFrom == attributeType;
        }

        public static bool ContainsVariable( this BinaryExpressionSyntax expression )
        {
            var matchKind = SyntaxKind.IdentifierName;
            if ( expression.Left.IsKind( matchKind ) || expression.Right.IsKind( matchKind ) )
                return true;

            if(expression.Left is BinaryExpressionSyntax)
                if ( ( expression.Left as BinaryExpressionSyntax ).ContainsVariable() )
                    return true;

            if ( expression.Right is BinaryExpressionSyntax )
                if ( ( expression.Right as BinaryExpressionSyntax ).ContainsVariable() )
                    return true;

            return false;
        }
    }
}

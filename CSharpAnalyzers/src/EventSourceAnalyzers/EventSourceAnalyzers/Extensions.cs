namespace EventSourceAnalyzers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    public static class Extensions
    {
        public static AttributeData GetAttributeOfType( this ISymbol method, string attributeTypeName, Compilation compilation )
        {
            if ( attributeTypeName == null ) throw new ArgumentNullException( nameof( attributeTypeName ) );

            var attributeType = compilation.GetTypeByMetadataName( attributeTypeName );
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

        public static INamedTypeSymbol GetAttribute( this SemanticModel semanticModel, AttributeArgumentSyntax attribArgument )
        {
            var argumentList = attribArgument.Parent as AttributeArgumentListSyntax;
            if ( argumentList == null )
                return null;

            var attributeSyntax = argumentList.Parent as AttributeSyntax;
            if ( attributeSyntax == null )
                return null;

            var attrib = semanticModel.GetSymbolInfo( attributeSyntax ).Symbol;
            if ( attrib == null )
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
            AttributeArgumentSyntax attributeArgument,
            string attributeTypeName )
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

            if ( expression.Left is BinaryExpressionSyntax )
                if ( ( expression.Left as BinaryExpressionSyntax ).ContainsVariable() )
                    return true;

            if ( expression.Right is BinaryExpressionSyntax )
                if ( ( expression.Right as BinaryExpressionSyntax ).ContainsVariable() )
                    return true;

            return false;
        }
    }
}
namespace EventSourceAnalyzers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
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
            const SyntaxKind matchKind = SyntaxKind.IdentifierName;
            if ( expression.Left.IsKind( matchKind ) || expression.Right.IsKind( matchKind ) )
                return true;

            var otherFound = false;

            if ( expression.Left is BinaryExpressionSyntax )
                otherFound = ( (BinaryExpressionSyntax)expression.Left ).ContainsVariable();

            if ( expression.Right is BinaryExpressionSyntax && !otherFound )
                otherFound = ( (BinaryExpressionSyntax)expression.Right ).ContainsVariable();

            if ( expression.Left is ParenthesizedExpressionSyntax && !otherFound )
                otherFound = ( (ParenthesizedExpressionSyntax)expression.Left ).ContainsVariable();

            if ( expression.Right is ParenthesizedExpressionSyntax && !otherFound )
                otherFound = ( (ParenthesizedExpressionSyntax)expression.Right ).ContainsVariable();

            return otherFound;
        }
        public static bool ContainsVariable( this ParenthesizedExpressionSyntax expression )
        {
            const SyntaxKind matchKind = SyntaxKind.IdentifierName;
            if ( expression.Expression.IsKind( matchKind ) ) return true;

            if ( ( expression.Expression is BinaryExpressionSyntax ) )
                if ( ( expression.Expression as BinaryExpressionSyntax ).ContainsVariable() )
                    return true;

            return false;
        }

        public static bool IsDerivedFrom( this ITypeSymbol type, ITypeSymbol toCheck )
        {
            if ( Equals( type.BaseType, toCheck ) )
                return true;

            if ( type.BaseType != null )
                return IsDerivedFrom( type.BaseType, toCheck );

            return false;
        }
    }
}
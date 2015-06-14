namespace EventSourceAnalyzers
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;

    public static class EventSourceSpecificExtensions
    {
        public static bool TryGetEventId( this IMethodSymbol method, out int eventId, Compilation compilation )
        {
            eventId = -1;

            var eventAttrib = method.GetAttributeOfType( EventSourceTypeNames.EventAttribute, compilation );
            if ( eventAttrib == null )
                return false;

            if ( eventAttrib.ConstructorArguments.Any() && eventAttrib.ConstructorArguments.First().Kind == TypedConstantKind.Primitive )
            {
                var argument = eventAttrib.ConstructorArguments.First();

                var value = (int)argument.Value;
                eventId = value;
                return true;
            }
            return true;
        }

        public static IEnumerable<int> GetUsedEventIds( this ITypeSymbol type, SemanticModel semanticModel )
        {
            var eventAttributeSymbol = EventSourceTypeSymbols.GetEventAttribute( semanticModel.Compilation );

            foreach ( var arg in type.GetMembers()
                .OfType<IMethodSymbol>()
                .SelectMany(
                    a =>
                        a.GetAttributes()
                            .Where( at => at.AttributeClass == eventAttributeSymbol )
                            .Select( att => att.ConstructorArguments.First( ac => ac.Value is int ) ) ) )
            {
                yield return (int)arg.Value;
            }
        }
    }
    
}
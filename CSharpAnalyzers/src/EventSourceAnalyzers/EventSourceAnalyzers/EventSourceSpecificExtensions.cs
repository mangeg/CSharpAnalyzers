namespace EventSourceAnalyzers
{
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
    }
}
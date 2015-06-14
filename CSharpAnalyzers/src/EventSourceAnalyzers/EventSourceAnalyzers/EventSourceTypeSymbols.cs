namespace EventSourceAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class EventSourceTypeSymbols
    {
        internal static ITypeSymbol GetEventAttribute( Compilation compilation )
        {
            return compilation.GetTypeByMetadataName( EventSourceTypeNames.EventAttribute );
        }
        internal static ITypeSymbol GetEventSourceAttribute( Compilation compilation )
        {
            return compilation.GetTypeByMetadataName( EventSourceTypeNames.EventSourceAttribute );
        }
        internal static ITypeSymbol GetEventSource( Compilation compilation )
        {
            return compilation.GetTypeByMetadataName( EventSourceTypeNames.EventSource );
        }
        internal static ITypeSymbol GetNonEventAttribute( Compilation compilation )
        {
            return compilation.GetTypeByMetadataName( EventSourceTypeNames.NonEventAttribute );
        }
    }
}
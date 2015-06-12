namespace EventSourceAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class EventSourceTypeSymbols
    {
        private static ITypeSymbol _eventAttribute;
        private static ITypeSymbol _eventSourceAttribute;
        private static ITypeSymbol _eventSource;

        internal static ITypeSymbol GetEventAttribute( Compilation compilation )
        {
            return _eventAttribute ?? ( _eventAttribute = compilation.GetTypeByMetadataName( EventSourceTypeNames.EventAttribute ) );
        }
        internal static ITypeSymbol GetEventSourceAttribute( Compilation compilation )
        {
            return _eventSourceAttribute ?? ( _eventSourceAttribute = compilation.GetTypeByMetadataName( EventSourceTypeNames.EventSourceAttribute ) );
        }
        internal static ITypeSymbol GetEventSource( Compilation compilation )
        {
            return _eventSource ?? ( _eventSource = compilation.GetTypeByMetadataName( EventSourceTypeNames.EventSource ) );
        }
    }
}
namespace DebugLib1
{
    using System.Diagnostics.Tracing;

    [EventSource]
    public class TestEvents : EventSource
    {
        private const int NormalEvents = 100;
        private const int SpecialEvent = 1000;

        [Event( SpecialEvent + 5 )]
        public void EvenThree( string arg1, string arg2, int test )
        {
            if ( IsEnabled() )
                WriteEvent( SpecialEvent + 5, arg1, arg2, test );
        }

        [Event( SpecialEvent + 5, Message = "Hello", Level = EventLevel.LogAlways, Keywords = (EventKeywords)2 )]
        public void EventOne(string input1, int inputVar2, int test )
        {
            if ( IsEnabled( EventLevel.LogAlways, (EventKeywords)2 ) )
                WriteEvent( SpecialEvent + 5, input1, inputVar2, test );
        }
    }
}

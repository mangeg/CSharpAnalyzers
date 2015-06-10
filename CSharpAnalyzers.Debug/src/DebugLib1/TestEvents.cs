namespace DebugLib1
{
    using System.Diagnostics.Tracing;

    [EventSource]
    public class TestEvents : EventSource
    {
        private const int NormalEvents = 100;

        [Event( NormalEvents + 1 )]
        public void EventOne( string arg1, string arg2 )
        {
            WriteEvent( NormalEvents + 1, arg1, arg2 );
        }
        [Event( NormalEvents + 2 )]
        public void EvenTwo( string arg1, string arg2 )
        {
            WriteEvent( NormalEvents + 10, arg1, arg2 );
        }
        [Event( NormalEvents + 1 + 3 - 2 )]
        public void EvenThree( string arg1, string arg2, int outer )
        {
            WriteEvent( NormalEvents + 1 + outer, arg1, arg2 );
        }
    }
}

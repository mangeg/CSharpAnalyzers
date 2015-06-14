# EventSource Analyzer
### Features
- Verify no duplicate event IDs used. With codefix that gives the next free event number including constant variable.
- Promote use of constant variables.
- Verify all input parameters passed to WriteEvent and in the correct order.
- Add WriteEvent when missing with the full parameter list. Supports simple, with enabled check and with detailed enabled check.
#### Samples
##### Wrong event ID and missing input param:
```C#
public class MyEventSource : EventSource
{
    private const int NormalEvents = 100;

    [Event( NormalEvents + 1 )]
    public void EventOne( string input1, string input2 )
    {
        WriteEvent( 312, input2 )
    }
}
```
###### Corrected:
- Correct to be the same as in EventAttribute.
- Correct and add all input paramters to the arguments.
```C#
public class MyEventSource : EventSource
{
    private const int NormalEvents = 100;

    [Event( NormalEvents + 1 )]
    public void EventOne( string input1, string input2 )
    {
        WriteEvent( NormalEvents + 1, input1, input2 )
    }
}
```
##### Input params passed in the wrong order:
###### Error:
```C#
public class MyEventSource : EventSource
{
    private const int NormalEvents = 100;

    [Event( NormalEvents + 1 )]
    public void EventOne( string input1, string input2 )
    {
        WriteEvent( NormalEvents + 1, input2, input1 )
    }
}
```
###### Corrected:
- Correct to be the same as in EventAttribute.
- Correct and add all input paramters to the arguments.
```C#
public class MyEventSource : EventSource
{
    private const int NormalEvents = 100;

    [Event( NormalEvents + 1 )]
    public void EventOne( string input1, string input2 )
    {
        WriteEvent( NormalEvents + 1, input1, input2 )
    }
}
```
##### Add call to WriteEvent:
```C#
public class MyEventSource : EventSource
{
    private const int NormalEvents = 100;

    [Event( NormalEvents + 1, Level = EventLevel.Error, Keywords = (EventKeywords)1 )]
    public void EventOne( string input1, string input2 )
    {
        // Simple
        WriteEvent( NormalEvents + 1, input1, input2 )
        // With enabled check
        if( IsEnabled() )
            WriteEvent( NormalEvents + 1, input1, input2 )
        // With detailed enabled check. Values picked from attribute.
        if ( IsEnabled( EventLevel.Error, (EventKeywords)1 ) )
            WriteEvent( NormalEvents + 1, input1, input2 )
    }
}
```

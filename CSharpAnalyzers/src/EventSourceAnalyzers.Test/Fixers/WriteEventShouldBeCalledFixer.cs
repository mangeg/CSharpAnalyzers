namespace EventSourceAnalyzers.Test.Fixers
{
    using CodeFixes;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using NUnit.Framework;
    using RoslynNUnitLight;

    [TestFixture]
    public class WriteEventShouldBeCalledFixer : CodeFixTestFixture
    {
        protected override string LanguageName => LanguageNames.CSharp;

        protected override CodeFixProvider CreateProvider() => new WriteEventShouldBeCalled();


        [Test]
        public void TestAddingWriteEventCallWithNoLevelAndKeywordSet()
        {
            var markupCode = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1)]
    public void EventOne(string input1, string input2)
    [|{
    }|]
}
";
            var expected1 = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1)]
    public void EventOne(string input1, string input2)
    {
        WriteEvent(NormalEvents + 1, input1, input2);
    }
}
";
            var expected2 = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1)]
    public void EventOne(string input1, string input2)
    {
        if (IsEnabled())
            WriteEvent(NormalEvents + 1, input1, input2);
    }
}
";
            var expected3 = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1)]
    public void EventOne(string input1, string input2)
    {
        if (IsEnabled(EventLevel.LogAlways, EventKeywords.None))
            WriteEvent(NormalEvents + 1, input1, input2);
    }
}
";
            TestCodeFix( markupCode, new[] { expected1, expected2, expected3 }, DiagnosticDescriptors.NoCalToWriteEvent );
        }

        [Test]
        public void TestAddingWriteEventCallWithNoKeywordSet()
        {
            var markupCode = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1, Level = EventLevel.Error)]
    public void EventOne(string input1, string input2)
    [|{
    }|]
}
";
            var expected1 = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1, Level = EventLevel.Error)]
    public void EventOne(string input1, string input2)
    {
        WriteEvent(NormalEvents + 1, input1, input2);
    }
}
";
            var expected2 = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1, Level = EventLevel.Error)]
    public void EventOne(string input1, string input2)
    {
        if (IsEnabled())
            WriteEvent(NormalEvents + 1, input1, input2);
    }
}
";
            var expected3 = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1, Level = EventLevel.Error)]
    public void EventOne(string input1, string input2)
    {
        if (IsEnabled(EventLevel.Error, EventKeywords.None))
            WriteEvent(NormalEvents + 1, input1, input2);
    }
}
";
            TestCodeFix( markupCode, new[] { expected1, expected2, expected3 }, DiagnosticDescriptors.NoCalToWriteEvent );
        }

        [Test]
        public void TestAddingWriteEventCallWithLevelAndKeywordSet()
        {
            var markupCode = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1, Level = EventLevel.Error, Keywords = (EventKeywords)1)]
    public void EventOne(string input1, string input2)
    [|{
    }|]
}
";
            var expected1 = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1, Level = EventLevel.Error, Keywords = (EventKeywords)1)]
    public void EventOne(string input1, string input2)
    {
        WriteEvent(NormalEvents + 1, input1, input2);
    }
}
";
            var expected2 = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1, Level = EventLevel.Error, Keywords = (EventKeywords)1)]
    public void EventOne(string input1, string input2)
    {
        if (IsEnabled())
            WriteEvent(NormalEvents + 1, input1, input2);
    }
}
";
            var expected3 = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1, Level = EventLevel.Error, Keywords = (EventKeywords)1)]
    public void EventOne(string input1, string input2)
    {
        if (IsEnabled(EventLevel.Error, (EventKeywords)1))
            WriteEvent(NormalEvents + 1, input1, input2);
    }
}
";
            TestCodeFix( markupCode, new[] { expected1, expected2, expected3 }, DiagnosticDescriptors.NoCalToWriteEvent );
        }

        [Test]
        public void TestAddingWriteEventCallWithKeywordSet()
        {
            var markupCode = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1, Keywords = (EventKeywords)1)]
    public void EventOne(string input1, string input2)
    [|{
    }|]
}
";
            var expected1 = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1, Keywords = (EventKeywords)1)]
    public void EventOne(string input1, string input2)
    {
        WriteEvent(NormalEvents + 1, input1, input2);
    }
}
";
            var expected2 = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1, Keywords = (EventKeywords)1)]
    public void EventOne(string input1, string input2)
    {
        if (IsEnabled())
            WriteEvent(NormalEvents + 1, input1, input2);
    }
}
";
            var expected3 = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1, Keywords = (EventKeywords)1)]
    public void EventOne(string input1, string input2)
    {
        if (IsEnabled(EventLevel.LogAlways, (EventKeywords)1))
            WriteEvent(NormalEvents + 1, input1, input2);
    }
}
";
            TestCodeFix( markupCode, new[] { expected1, expected2, expected3 }, DiagnosticDescriptors.NoCalToWriteEvent );
        }
    }
}



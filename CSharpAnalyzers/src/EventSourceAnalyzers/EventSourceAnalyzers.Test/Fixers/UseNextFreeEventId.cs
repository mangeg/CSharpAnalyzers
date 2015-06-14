namespace EventSourceAnalyzers.Test.Fixers
{
    using CodeFixes;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using NUnit.Framework;
    using RoslynNUnitLight;

    [TestFixture]
    public class UseNextFreeEventId : CodeFixTestFixture
    {
        protected override string LanguageName => LanguageNames.CSharp;

        protected override CodeFixProvider CreateProvider() => new EventIdUsedFixer();

        [Test]
        public void TestNextFreeEventIdWithSingleConstant()
        {
            var markupCode = @"
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
    [Event(NormalEvents + 1)]
    public void [|EventTwo|](string input1, string input2)
    {
        WriteEvent(NormalEvents + 1, input1, input2);
    }
}
";
            var expected = @"
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
    [Event(NormalEvents + 2)]
    public void EventTwo(string input1, string input2)
    {
        WriteEvent(NormalEvents + 2, input1, input2);
    }
}
";
            TestCodeFix( markupCode, expected, DiagnosticDescriptors.EventNumberUsedMultipleTimes );
        }

        [Test]
        public void TestNextFreeEventIdWithMultipleConstant()
        {
            var markupCode = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;
    private const int SpecialEvents = 1000;

    [Event(NormalEvents + 1)]
    public void EventOne(string input1, string input2)
    {
        WriteEvent(NormalEvents + 1, input1, input2);
    }
    [Event(NormalEvents + 1)]
    public void [|EventTwo|](string input1, string input2)
    {
        WriteEvent(NormalEvents + 1, input1, input2);
    }
}
";
            var expected1 = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;
    private const int SpecialEvents = 1000;

    [Event(NormalEvents + 1)]
    public void EventOne(string input1, string input2)
    {
        WriteEvent(NormalEvents + 1, input1, input2);
    }
    [Event(NormalEvents + 2)]
    public void EventTwo(string input1, string input2)
    {
        WriteEvent(NormalEvents + 2, input1, input2);
    }
}
";
            var expected2 = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;
    private const int SpecialEvents = 1000;

    [Event(NormalEvents + 1)]
    public void EventOne(string input1, string input2)
    {
        WriteEvent(NormalEvents + 1, input1, input2);
    }
    [Event(SpecialEvents + 1)]
    public void EventTwo(string input1, string input2)
    {
        WriteEvent(SpecialEvents + 1, input1, input2);
    }
}
";
            TestCodeFix( markupCode, new[] { expected1, expected2 }, DiagnosticDescriptors.EventNumberUsedMultipleTimes );
        }
    }
}
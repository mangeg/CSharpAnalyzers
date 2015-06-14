namespace EventSourceAnalyzers.Test.Fixers
{
    using CodeFixes;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using NUnit.Framework;
    using RoslynNUnitLight;

    [TestFixture]
    public class MethodsShouldHaveAttributesTests : CodeFixTestFixture
    {
        protected override string LanguageName => LanguageNames.CSharp;

        protected override CodeFixProvider CreateProvider() => new MethodShouldHaveAttributeFixer();

        [Test]
        public void TestFixAddAttributeToFirstMethod()
        {
            var markupCode = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    public void [|EventOne|](string input1, string input2)
    {
        WriteEvent(NormalEvents + 2, input1, input2);
    }
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

    [NonEvent]
    public void EventOne(string input1, string input2)
    {
        WriteEvent(NormalEvents + 2, input1, input2);
    }
}
";
            TestCodeFix( markupCode, new[] { expected1, expected2 }, DiagnosticDescriptors.MethodsShouldHaveAttributes );
        }

        [Test]
        public void TestFixAddAttributeToMethodWithAllreadyExistingMethod()
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
        WriteEvent(NormalEvents + 2], input1, input2);
    }
    public void [|EventTwo|](string input1, string input2)
    {
        WriteEvent(NormalEvents + 2, input1, input2);
    }
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
        WriteEvent(NormalEvents + 2], input1, input2);
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

    [Event(NormalEvents + 1)]
    public void EventOne(string input1, string input2)
    {
        WriteEvent(NormalEvents + 2], input1, input2);
    }

    [NonEvent]
    public void EventTwo(string input1, string input2)
    {
        WriteEvent(NormalEvents + 2, input1, input2);
    }
}
";
            TestCodeFix( markupCode, new[] { expected1, expected2 }, DiagnosticDescriptors.MethodsShouldHaveAttributes );
        }

        [Test]
        public void TestFixAddAttributeToMethodWithAllreadyExistingMethodMultipleConstants()
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
        WriteEvent(NormalEvents + 2, input1, input2);
    }
    public void [|EventTwo|](string input1, string input2)
    {
        WriteEvent(NormalEvents + 2, input1, input2);
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
        WriteEvent(NormalEvents + 2, input1, input2);
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
        WriteEvent(NormalEvents + 2, input1, input2);
    }

    [Event(SpecialEvents + 1)]
    public void EventTwo(string input1, string input2)
    {
        WriteEvent(NormalEvents + 2, input1, input2);
    }
}
";
            var expected3 = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;
    private const int SpecialEvents = 1000;

    [Event(NormalEvents + 1)]
    public void EventOne(string input1, string input2)
    {
        WriteEvent(NormalEvents + 2, input1, input2);
    }

    [NonEvent]
    public void EventTwo(string input1, string input2)
    {
        WriteEvent(NormalEvents + 2, input1, input2);
    }
}
";
            TestCodeFix( markupCode, new[] { expected1, expected2, expected3 }, DiagnosticDescriptors.MethodsShouldHaveAttributes );
        }
    }
}
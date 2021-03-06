namespace EventSourceAnalyzers.Test.Analyzers
{
    using EventSourceAnalyzers.Analyzers;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;
    using RoslynNUnitLight;

    [TestFixture]
    public class AllMethodsShouldHaveAttributes : AnalyzerTestFixture
    {
        protected override string LanguageName => Microsoft.CodeAnalysis.LanguageNames.CSharp;
        protected override DiagnosticAnalyzer CreateAnalyzer() => new EventSourceAnalyzers();

        [Test]
        public void NoDiagnosticAllMethodsHaveAttribute()
        {
            var code = @"
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
            NoDiagnostic( code, DiagnosticIds.MethodShouldHaveAttributes );
        }

        [Test]
        public void DiagnosticNotAllMethodsHaveAttribute()
        {
            var code = @"
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
    
    public void [|EventTwo|](string input1, string input2)
    {
        WriteEvent(NormalEvents + 2, input1, input2);
    }
}
";
            HasDiagnostic( code, DiagnosticIds.MethodShouldHaveAttributes );
        }
    }

    [TestFixture]
    public class EventIdUsedMultipleTimes : AnalyzerTestFixture
    {
        protected override string LanguageName => Microsoft.CodeAnalysis.LanguageNames.CSharp;
        protected override DiagnosticAnalyzer CreateAnalyzer() => new EventSourceAnalyzers();

        [Test]
        public void NoDiagnosticNoDuplicateEventId()
        {
            var code = @"
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
            NoDiagnostic( code, DiagnosticIds.EventNumberUsedMultipleTimes );
        }

        [Test]
        public void DiagnosticWhenDuplicateEventId()
        {
            var code = @"
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
            HasDiagnostic( code, DiagnosticIds.EventNumberUsedMultipleTimes );
        }

        [Test]
        public void DiagnosticWhenDuplicateEventIdOnMultipleMethods()
        {
            var code = @"
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
    public void EventTwo(string input1, string input2)
    {
        WriteEvent(NormalEvents + 1, input1, input2);
    }

    [Event(NormalEvents + 1)]
    public void [|EventThree|](string input1, string input2)
    {
        WriteEvent(NormalEvents + 1, input1, input2);
    }
}
";
            HasDiagnostic( code, DiagnosticIds.EventNumberUsedMultipleTimes );
        }

        [Test]
        public void NoDiagnosticForStaticMethods()
        {
            var code = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    public static void Test()
    {
    }
}
";
            NoDiagnostic( code, DiagnosticIds.MethodShouldHaveAttributes );
        }

        [Test]
        public void NoDiagnosticForNonPublicMethods()
        {
            var code = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    private void Test()
    {
    }

    protected void Test2()
    {
    }
}
";
            NoDiagnostic( code, DiagnosticIds.MethodShouldHaveAttributes );
        }
    }
}
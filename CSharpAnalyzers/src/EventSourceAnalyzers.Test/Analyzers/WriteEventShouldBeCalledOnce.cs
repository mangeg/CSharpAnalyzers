namespace EventSourceAnalyzers.Test.Analyzers
{
    using EventSourceAnalyzers.Analyzers;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;
    using RoslynNUnitLight;

    [TestFixture]
    public class WriteEventShouldBeCalledOnce : AnalyzerTestFixture
    {
        protected override string LanguageName => Microsoft.CodeAnalysis.LanguageNames.CSharp;
        protected override DiagnosticAnalyzer CreateAnalyzer() => new EventSourceAnalyzers();

        [Test]
        public void NoDiagnosticWriteEventIsCalledOnce()
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
}
";
            NoDiagnostic( code, DiagnosticIds.NoCallToWriteEvent );
        }


        [Test]
        public void DiagnosticWriteEventIsNotCalled()
        {
            var code = @"
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
            HasDiagnostic( code, DiagnosticIds.NoCallToWriteEvent );
        }

        [Test]
        public void DiagnosticWriteEventIsCalledMultipleTimes()
        {
            var code = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1)]
    public void EventOne(string input1, string input2)
    [|{
        WriteEvent(NormalEvents + 1, input1, input2);
        WriteEvent(NormalEvents + 1, input1, input2);
    }|]
}
";
            HasDiagnostic( code, DiagnosticIds.MultipleCallToWriteEvent );
        }
    }
}
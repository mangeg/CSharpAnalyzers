namespace EventSourceAnalyzers.Test.Analyzers
{
    using EventSourceAnalyzers.Analyzers;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;
    using RoslynNUnitLight;

    [TestFixture]
    public class EventIdShouldContainConstant : AnalyzerTestFixture
    {
        protected override string LanguageName => Microsoft.CodeAnalysis.LanguageNames.CSharp;
        protected override DiagnosticAnalyzer CreateAnalyzer() => new EventSourceAnalyzers();

        [Test]
        public void NoDiagnosticWhenConstantAdderInEventId()
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
            NoDiagnostic( code, DiagnosticIds.UseConstantAddersForEventId );
        }

        [Test]
        public void NoDiagnosticWhenConstantAdderInEventIdWithOperators()
        {
            var code = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event((1) + (NormalEvents * 10) + (1 * 10) + (NormalEvents) + (2))]
    public void EventOne(string input1, string input2)
    {
        WriteEvent((1) + (NormalEvents * 10) + (1 * 10) + (NormalEvents) + (2), input1, input2);
    }
}
";
            NoDiagnostic( code, DiagnosticIds.UseConstantAddersForEventId );
        }

        [Test]
        public void DiagnosticWhenNoConstantAdderInEventId()
        {
            var code = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event([|1|])]
    public void EventOne(string input1, string input2)
    {
        WriteEvent(1, input1, input2);
    }
}
";
            HasDiagnostic( code, DiagnosticIds.UseConstantAddersForEventId );
        }
    }
}
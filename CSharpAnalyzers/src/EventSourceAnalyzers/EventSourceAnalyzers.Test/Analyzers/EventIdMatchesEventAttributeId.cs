namespace EventSourceAnalyzers.Test.Analyzers
{
    using EventSourceAnalyzers.Analyzers;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;
    using RoslynNUnitLight;

    [TestFixture]
    public class EventIdMatchesEventAttributeId : AnalyzerTestFixture
    {
        protected override string LanguageName => Microsoft.CodeAnalysis.LanguageNames.CSharp;
        protected override DiagnosticAnalyzer CreateAnalyzer() => new EventSourceAnalyzers();

        [Test]
        public void NoDiagnosticWhenIdMatches()
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

            NoDiagnostic( code, DiagnosticIds.CallToWriteEventMustUseSameEventId );

        }

        [Test]
        public void DiagnosticWhenIdDoesNotMatches()
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
        WriteEvent([|NormalEvents + 2|], input1, input2);
    }
}
";
            HasDiagnostic( code, DiagnosticIds.CallToWriteEventMustUseSameEventId );
        }
    }
}
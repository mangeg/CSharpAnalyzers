namespace EventSourceAnalyzers.Test.Analyzers
{
    using EventSourceAnalyzers.Analyzers;
    using Microsoft.CodeAnalysis.Diagnostics;
    using NUnit.Framework;
    using RoslynNUnitLight;

    [TestFixture]
    public class WriteEventShouldUseConstantId : AnalyzerTestFixture
    {
        protected override string LanguageName => Microsoft.CodeAnalysis.LanguageNames.CSharp;
        protected override DiagnosticAnalyzer CreateAnalyzer() => new EventSourceAnalyzers();

        [Test]
        public void NoDiagnosticWhenIdToWriteEventIsConstat()
        {
            var code = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1)]
    public void EventOne(string input1, string input2, int outer)
    {
        WriteEvent(NormalEvents + 1, input1, input2, outer);
    }
}
";
            NoDiagnostic( code, DiagnosticIds.CallToWriteEventIdShouldBeConstant );
        }

        [Test]
        public void DiagnosticWhenIdToWriteEventIsNotConstat()
        {
            var code = @"
using System.Diagnostics.Tracing;

[EventSource]
class TestSource : EventSource
{
    private const int NormalEvents = 100;

    [Event(NormalEvents + 1)]
    public void EventOne(string input1, string input2, int outer)
    {
        WriteEvent([|NormalEvents + 1 + outer|], input1, input2, outer);
    }
}
";
            HasDiagnostic( code, DiagnosticIds.CallToWriteEventIdShouldBeConstant );
        }
    }
}
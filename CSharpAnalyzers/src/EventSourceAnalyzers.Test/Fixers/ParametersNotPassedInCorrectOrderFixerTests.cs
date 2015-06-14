namespace EventSourceAnalyzers.Test.Fixers
{
    using CodeFixes;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using NUnit.Framework;
    using RoslynNUnitLight;

    [TestFixture]
    public class ParametersNotPassedInCorrectOrderFixerTests : CodeFixTestFixture
    {
        protected override string LanguageName => LanguageNames.CSharp;

        protected override CodeFixProvider CreateProvider() => new ParametersNotPassedInTheSameOrderFixer();

        [Test]
        public void TestFixParametersNotInCorrectOrder()
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
        WriteEvent(NormalEvents + 2, [|input2, input1|]);
    }
}
";
            var expected = @"
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
}
";
            TestCodeFix( markupCode, expected, DiagnosticDescriptors.ParametersNotPassedInTheSameOrder );
        }

        [Test]
        public void TestFixNotAllParametersPassed()
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
        WriteEvent(NormalEvents + 2, [|input1|]);
    }
}
";
            var expected = @"
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
}
";
            TestCodeFix( markupCode, expected, DiagnosticDescriptors.NotAllInputParametersPassed );
        }
    }
}
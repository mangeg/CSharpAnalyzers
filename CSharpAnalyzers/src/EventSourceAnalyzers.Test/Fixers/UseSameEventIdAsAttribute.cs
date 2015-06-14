namespace EventSourceAnalyzers.Test.Fixers
{
    using CodeFixes;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using NUnit.Framework;
    using RoslynNUnitLight;

    [TestFixture]
    public class UseSameEventIdAsAttribute : CodeFixTestFixture
    {
        protected override string LanguageName => LanguageNames.CSharp;

        protected override CodeFixProvider CreateProvider() => new UseSameEventIdCodeFixer();

        [Test]
        public void TestFixUseEventIdInSingleWriteEvent()
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
        WriteEvent([|NormalEvents + 2|], input1, input2);
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
        WriteEvent(NormalEvents + 1, input1, input2);
    }
}
";
            TestCodeFix( markupCode, expected1, DiagnosticDescriptors.CallToWriteEventMustUseSameEventId );
        }
    }
}
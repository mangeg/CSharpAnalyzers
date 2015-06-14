namespace EventSourceAnalyzers.Test.Fixers
{
    using System;
    using CodeFixes;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.VisualBasic;
    using NUnit.Framework;
    using RoslynNUnitLight;

    [TestFixture]
    public class UserConstantAdder : CodeFixTestFixture
    {
        protected override string LanguageName => LanguageNames.CSharp;

        protected override CodeFixProvider CreateProvider()  => new UseConstantAdderFixer();


        [Test]
        public void TestAddingConstantAdderToAttribute()
        {
            var markupCode = @"
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
}
";
            TestCodeFix( markupCode, expected, DiagnosticDescriptors.UseConstantAddersForEventId );
        }

    }
}
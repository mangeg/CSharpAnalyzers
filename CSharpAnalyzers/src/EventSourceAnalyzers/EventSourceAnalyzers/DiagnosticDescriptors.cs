namespace EventSourceAnalyzers
{
    using Microsoft.CodeAnalysis;

    internal static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor EventNumberUsedMultipleTimes =
            new DiagnosticDescriptor(
                DiagnosticIds.EventNumberUsedMultipleTimes,
                "Each event method requires a unique id",
                "Duplicate event ID {0} in method(s) {1}",
                DiagnosticCategories.Language,
                DiagnosticSeverity.Error,
                true );
        public static readonly DiagnosticDescriptor UseConstantAddersForEventId =
            new DiagnosticDescriptor(
                DiagnosticIds.UseConstantAddersForEventId,
                "Use constants + number for event id",
                "Consider use of constant + number for event id",
                DiagnosticCategories.Language,
                DiagnosticSeverity.Warning,
                true
                );
        public static readonly DiagnosticDescriptor CallToWriteEventMustUseSameEventId =
            new DiagnosticDescriptor(
                DiagnosticIds.CallToWriteEventMustUseSameEventId,
                "The ID used in WriteEvent must be the same as in the EventAttribute",
                "Event ID {0} used in WriteEvent is different from EventAttributes {1}",
                DiagnosticCategories.Language,
                DiagnosticSeverity.Error,
                true
                );
        public static readonly DiagnosticDescriptor CallToWriteEventIdShouldBeConstant =
            new DiagnosticDescriptor(
                DiagnosticIds.CallToWriteEventIdShouldBeConstant,
                "The ID used in WriteEvent should be a constant value",
                "Event ID used in WriteEvent is not constant.",
                DiagnosticCategories.Language,
                DiagnosticSeverity.Warning,
                true
                );
    }
}
namespace EventSourceAnalyzers
{
    internal static class DiagnosticIds
    {
        public const string EventNumberUsedMultipleTimes = "MES0001";
        public const string UseConstantAddersForEventId = "MES0002";
        public const string CallToWriteEventMustUseSameEventId = "MES0003";
        public const string CallToWriteEventIdShouldBeConstant = "MES0004";
    }
}
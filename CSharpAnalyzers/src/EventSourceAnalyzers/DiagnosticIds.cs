namespace EventSourceAnalyzers
{
    internal static class DiagnosticIds
    {
        public const string EventNumberUsedMultipleTimes = "MES0001";
        public const string UseConstantAddersForEventId = "MES0002";
        public const string CallToWriteEventMustUseSameEventId = "MES0003";
        public const string CallToWriteEventIdShouldBeConstant = "MES0004";
        public const string MultipleCallToWriteEvent = "MES0005";
        public const string NoCallToWriteEvent = "MES0006";
        public const string ParametersNotPassedInTheSameOrder = "MES0007";
        public const string NotAllInputParametersPassed = "MES0008";
        public const string MethodShouldHaveAttributes = "MES0009";
    }
}
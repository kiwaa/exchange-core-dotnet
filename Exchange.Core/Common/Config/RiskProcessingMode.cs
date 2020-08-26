namespace Exchange.Core.Common.Config
{
    public enum RiskProcessingMode
    {
        // risk processing is on, every currency/asset account is checked independently
        FULL_PER_CURRENCY,

        // risk processing is off, any orders accepted and placed
        NO_RISK_PROCESSING
    }
}
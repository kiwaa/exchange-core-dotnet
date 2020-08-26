using System;

namespace Exchange.Core.Common.Config
{
    [Flags]
    public enum LoggingLevel
    {
        LOGGING_WARNINGS,
        LOGGING_RISK_DEBUG,
        LOGGING_MATCHING_DEBUG
    }
}
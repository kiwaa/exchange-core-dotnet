using System;

namespace Exchange.Core.Common.Config
{
    public sealed partial class OrdersProcessingConfiguration
    {
        public static OrdersProcessingConfiguration DEFAULT = Builder()
               .riskProcessingMode(RiskProcessingMode.FULL_PER_CURRENCY)
               .marginTradingMode(MarginTradingMode.MARGIN_TRADING_ENABLED)
               .build();
    }
}
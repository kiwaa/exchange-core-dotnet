using System;

namespace Exchange.Core.Common.Config
{
    public class OrdersProcessingConfiguration
    {
        public static OrdersProcessingConfiguration DEFAULT = builder()
               .riskProcessingMode(RiskProcessingMode.FULL_PER_CURRENCY)
               .marginTradingMode(MarginTradingMode.MARGIN_TRADING_ENABLED)
               .build();

        private static OrdersProcessingConfigurationBuilder builder()
        {
            throw new NotImplementedException();
        }

        private class OrdersProcessingConfigurationBuilder
        {
            internal OrdersProcessingConfiguration build()
            {
                throw new NotImplementedException();
            }

            internal OrdersProcessingConfigurationBuilder marginTradingMode(MarginTradingMode mARGIN_TRADING_ENABLED)
            {
                throw new NotImplementedException();
            }

            internal OrdersProcessingConfigurationBuilder riskProcessingMode(RiskProcessingMode fULL_PER_CURRENCY)
            {
                throw new NotImplementedException();
            }
        }
    }
}
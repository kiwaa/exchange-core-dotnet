using System;

namespace Exchange.Core.Common.Config
{
    public class ExchangeConfiguration
    {
        /*
        */
        public OrdersProcessingConfiguration OrdersProcessingCfg { get; }

        /*
         * Performance configuration
         */
        public PerformanceConfiguration PerformanceCfg { get; }

        /*
         * Exchange initialization configuration
         */
        public InitialStateConfiguration InitStateCfg { get; }

        /*
         * Exchange configuration
         */
        public ReportsQueriesConfiguration ReportsQueriesCfg { get; }

        /*
         * Logging configuration
         */
        public LoggingConfiguration LoggingCfg { get; }

        /*
         * Serialization (snapshots and journaling) configuration
         */
        public SerializationConfiguration SerializationCfg { get; }

        public ExchangeConfiguration(OrdersProcessingConfiguration ordersProcessingCfg, PerformanceConfiguration performanceCfg, InitialStateConfiguration initStateCfg, ReportsQueriesConfiguration reportsQueriesCfg, LoggingConfiguration loggingCfg, SerializationConfiguration serializationCfg)
        {
            OrdersProcessingCfg = ordersProcessingCfg;
            PerformanceCfg = performanceCfg;
            InitStateCfg = initStateCfg;
            ReportsQueriesCfg = reportsQueriesCfg;
            LoggingCfg = loggingCfg;
            SerializationCfg = serializationCfg;
        }

        public override string ToString()
        {
            return "ExchangeConfiguration{" +
                    "\n  ordersProcessingCfg=" + OrdersProcessingCfg +
                    "\n  performanceCfg=" + PerformanceCfg +
                    "\n  initStateCfg=" + InitStateCfg +
                    "\n  reportsQueriesCfg=" + ReportsQueriesCfg +
                    "\n  loggingCfg=" + LoggingCfg +
                    "\n  serializationCfg=" + SerializationCfg +
                    '}';
        }

        public static ExchangeConfigurationBuilder builder()
        {
            return new ExchangeConfigurationBuilder();
        }

        public class ExchangeConfigurationBuilder
        {
            private OrdersProcessingConfiguration _ordersProcessingConfiguration;
            private PerformanceConfiguration _performanceConfiguration;
            private InitialStateConfiguration _initialStateConfiguration;
            private ReportsQueriesConfiguration _reportsQueriesConfiguration;
            private LoggingConfiguration _loggingConfiguration;
            private SerializationConfiguration _serializationConfiguration;

            public ExchangeConfiguration build()
            {
                throw new NotImplementedException();
            }

            public ExchangeConfigurationBuilder ordersProcessingCfg(OrdersProcessingConfiguration value)
            {
                _ordersProcessingConfiguration = value;
                return this;
            }

            public ExchangeConfigurationBuilder performanceCfg(PerformanceConfiguration value)
            {
                _performanceConfiguration = value;
                return this;
            }

            public ExchangeConfigurationBuilder initStateCfg(InitialStateConfiguration value)
            {
                _initialStateConfiguration = value;
                return this;
            }

            public ExchangeConfigurationBuilder reportsQueriesCfg(ReportsQueriesConfiguration value)
            {
                _reportsQueriesConfiguration = value;
                return this;
            }

            public ExchangeConfigurationBuilder loggingCfg(LoggingConfiguration value)
            {
                _loggingConfiguration = value;
                return this;
            }
            public ExchangeConfigurationBuilder serializationCfg(SerializationConfiguration value)
            {
                _serializationConfiguration = value;
                return this;
            }
        }

        public static ExchangeConfigurationBuilder defaultBuilder()
        {
            return builder()
                  .ordersProcessingCfg(OrdersProcessingConfiguration.DEFAULT)
                  .initStateCfg(InitialStateConfiguration.DEFAULT)
                  .performanceCfg(PerformanceConfiguration.DEFAULT)
                  .reportsQueriesCfg(ReportsQueriesConfiguration.DEFAULT)
                  .loggingCfg(LoggingConfiguration.DEFAULT)
                  .serializationCfg(SerializationConfiguration.DEFAULT);
        }

    }
}
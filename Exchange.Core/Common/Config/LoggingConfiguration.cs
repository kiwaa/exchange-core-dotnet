namespace Exchange.Core.Common.Config
{
    public class LoggingConfiguration
    {
        // only warnings
        public static LoggingConfiguration DEFAULT = builder()
                .loggingLevels(LoggingLevel.LOGGING_WARNINGS)
                .build();


        public LoggingLevel LoggingLevels { get; }

        public LoggingConfiguration(LoggingLevel loggingLevels)
        {
            LoggingLevels = loggingLevels;
        }

        private static LoggingConfigurationBuilder builder()
        {
            return new LoggingConfigurationBuilder();
        }

        private class LoggingConfigurationBuilder
        {
            private LoggingLevel _loggingLevels;

            internal LoggingConfigurationBuilder loggingLevels(LoggingLevel levels)
            {
                _loggingLevels = levels;
                return this;
            }

            internal LoggingConfiguration build()
            {
                return new LoggingConfiguration(_loggingLevels);
            }
        }
    }
}
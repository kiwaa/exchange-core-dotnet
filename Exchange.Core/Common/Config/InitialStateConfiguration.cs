using System;

namespace Exchange.Core.Common.Config
{
    public class InitialStateConfiguration
    {
        public static InitialStateConfiguration DEFAULT = cleanStart("MY_EXCHANGE");

        /**
         * Clean start configuration
         *
         * @param exchangeId Exchange ID
         * @return clean start configuration without journaling.
         */
        public static InitialStateConfiguration cleanStart(string exchangeId)
        {

            return builder()
                    .exchangeId(exchangeId)
                    .snapshotId(0)
                    .build();
        }

        private static InitialStateConfigurationBuilder builder()
        {
            throw new NotImplementedException();
        }

        private class InitialStateConfigurationBuilder
        {
            internal InitialStateConfiguration build()
            {
                throw new NotImplementedException();
            }

            internal InitialStateConfigurationBuilder exchangeId(string exchangeId)
            {
                throw new NotImplementedException();
            }

            internal InitialStateConfigurationBuilder snapshotId(int v)
            {
                throw new NotImplementedException();
            }
        }
    }
}
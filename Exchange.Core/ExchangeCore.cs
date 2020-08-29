using Exchange.Core.Common.Config;
using System;

namespace Exchange.Core
{
    public class ExchangeCore
    {  
        // enable MatcherTradeEvent pooling
        public static readonly bool EVENTS_POOLING = false;

        internal static ExchangeCoreBuilder builder()
        {
            throw new NotImplementedException();
        }

        internal void startup()
        {
            throw new NotImplementedException();
        }

        internal ExchangeApi getApi()
        {
            throw new NotImplementedException();
        }

        internal class ExchangeCoreBuilder
        {
            internal ExchangeCoreBuilder resultsConsumer(SimpleEventsProcessor eventsProcessor)
            {
                return this;
            }

            internal ExchangeCoreBuilder exchangeConfiguration(ExchangeConfiguration conf)
            {
                return this;
            }

            internal ExchangeCore build()
            {
                throw new NotImplementedException();
            }
        }
    }
}
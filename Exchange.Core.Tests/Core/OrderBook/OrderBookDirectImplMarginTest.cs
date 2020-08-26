using Exchange.Core.Common;
using Exchange.Core.Common.Config;
using Exchange.Core.Orderbook;
using ObjectsPool = Exchange.Core.Collections.ObjPool.NaiveObjectsPool;

namespace Exchange.Core.Tests.Core.OrderBook
{
    public sealed class OrderBookDirectImplMarginTest : OrderBookDirectImplTest
    {

        protected override IOrderBook createNewOrderBook()
        {
            return new OrderBookDirectImpl(
                    getCoreSymbolSpec(),
                    ObjectsPool.createDefaultTestPool(),
                    OrderBookEventsHelper.NON_POOLED_EVENTS_HELPER,
                    LoggingConfiguration.DEFAULT);
        }

        protected override CoreSymbolSpecification getCoreSymbolSpec()
        {
            return TestConstants.SYMBOLSPEC_EUR_USD;
        }

    }
}

using Exchange.Core.Collections.ObjPool;
using Exchange.Core.Common;
using Exchange.Core.Common.Config;
using Exchange.Core.Orderbook;
using Exchange.Core.Tests.Utils;
using NUnit.Framework;
using ObjectsPool = Exchange.Core.Collections.ObjPool.NaiveObjectsPool;

namespace Exchange.Core.Tests.Core.OrderBook
{
    [TestFixture]
    public sealed class OrderBookDirectImplExchangeTest : OrderBookDirectImplTest
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
            return TestConstants.SYMBOLSPECFEE_XBT_LTC;
        }
    }
}

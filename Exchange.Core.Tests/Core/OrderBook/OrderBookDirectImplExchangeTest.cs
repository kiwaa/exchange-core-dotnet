using Exchange.Core.Collections.ObjPool;
using Exchange.Core.Common;
using Exchange.Core.Common.Config;
using Exchange.Core.Orderbook;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Core.OrderBook
{
    [TestFixture]
    public sealed class OrderBookDirectImplExchangeTest : OrderBookBaseTest
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

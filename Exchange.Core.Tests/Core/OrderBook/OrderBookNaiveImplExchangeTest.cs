using Exchange.Core.Common;
using Exchange.Core.Common.Config;
using Exchange.Core.Orderbook;
using Exchange.Core.Tests.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Core.OrderBook
{
    [TestFixture]
    public sealed class OrderBookNaiveImplExchangeTest : OrderBookBaseTest
    {
        protected override IOrderBook createNewOrderBook()
        {
            return new OrderBookNaiveImpl(getCoreSymbolSpec(), LoggingConfiguration.DEFAULT);
        }

        protected override CoreSymbolSpecification getCoreSymbolSpec()
        {
            return TestConstants.SYMBOLSPEC_ETH_XBT;
        }


    }

}

using Exchange.Core.Common;
using Exchange.Core.Common.Config;
using Exchange.Core.Orderbook;
using Exchange.Core.Tests.Utils;
using NUnit.Framework;

namespace Exchange.Core.Tests.Core.OrderBook
{
    [TestFixture]
    public sealed class OrderBookNaiveImplMarginTest : OrderBookBaseTest
    {
        protected override IOrderBook createNewOrderBook()
        {
            return new OrderBookNaiveImpl(getCoreSymbolSpec(), LoggingConfiguration.DEFAULT);
        }

        protected override CoreSymbolSpecification getCoreSymbolSpec()
        {
            return TestConstants.SYMBOLSPEC_EUR_USD;
        }

    }
}

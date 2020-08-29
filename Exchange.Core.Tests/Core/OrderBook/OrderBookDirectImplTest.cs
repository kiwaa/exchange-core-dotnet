using Exchange.Core.Common;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Common.Config;
using Exchange.Core.Orderbook;
using Exchange.Core.Tests.Utils;
using Exchange.Core.Utils;
using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Core.OrderBook
{
    public abstract class OrderBookDirectImplTest : OrderBookBaseTest
    {
        private static ILog log = LogManager.GetLogger(typeof(OrderBookDirectImplTest));

        [Test]
        public void multipleCommandsCompareTest()
        {

            // TODO more efficient - multi-threaded executions with different seed and order book type

            DateTime nextUpdateTime = DateTime.MinValue;

            int tranNum = 100_000;
            int targetOrderBookOrders = 500;
            int numUsers = 100;

            IOrderBook orderBook = createNewOrderBook();
            //        IOrderBook orderBook = new OrderBookFastImpl(4096, TestConstants.SYMBOLSPEC_EUR_USD);
            //IOrderBook orderBook = new OrderBookNaiveImpl();
            IOrderBook orderBookRef = new OrderBookNaiveImpl(getCoreSymbolSpec(), LoggingConfiguration.DEFAULT);

            Assert.AreEqual(orderBook.stateHash(), orderBookRef.stateHash());

            GenResult genResult = TestOrdersGenerator.generateCommands(
                    tranNum,
                    targetOrderBookOrders,
                    numUsers,
                    TestOrdersGenerator.UID_PLAIN_MAPPER,
                    0,
                    true,
                    false,
                    TestOrdersGenerator.createAsyncProgressLogger(tranNum),
                    1825793762);

            long i = 0;
            foreach (OrderCommand cmd in genResult.getCommands())
            {
                i++;
                cmd.OrderId += 100;

                cmd.ResultCode = CommandResultCode.VALID_FOR_MATCHING_ENGINE;
                IOrderBook.processCommand(orderBook, cmd);

                cmd.ResultCode = CommandResultCode.VALID_FOR_MATCHING_ENGINE;
                CommandResultCode commandResultCode = IOrderBook.processCommand(orderBookRef, cmd);

                Assert.AreEqual(commandResultCode, CommandResultCode.SUCCESS);

                //            if (!orderBook.equals(orderBookRef)) {
                //
                //                if (!orderBook.getAllAskBuckets().equals(orderBookRef.getAllAskBuckets())) {
                //                    log.warn("ASK FAST: {}", orderBook.getAllAskBuckets());
                //                    log.warn("ASK REF : {}", orderBookRef.getAllAskBuckets());
                //                } else {
                //                    log.info("ASK ok");
                //                }
                //
                //                if (!orderBook.getAllBidBuckets().equals(orderBookRef.getAllBidBuckets())) {
                //                    log.warn("BID FAST: {}", orderBook.getAllBidBuckets().stream().map(x -> x.getPrice() + " " + x.getTotalVolume()).toArray());
                //                    log.warn("BID REF : {}", orderBookRef.getAllBidBuckets().stream().map(x -> x.getPrice() + " " + x.getTotalVolume()).toArray());
                //                } else {
                //                    log.info("BID ok");
                //                }
                //
                //            }

                if (i % 100 == 0)
                {
                    Assert.AreEqual(orderBook.stateHash(), orderBookRef.stateHash());
                    //            assertTrue(checkSameOrders(orderBook, orderBookRef));
                }

                // TODO compare events!
                // TODO compare L2 marketdata

                if (DateTime.UtcNow > nextUpdateTime)
                {
                    log.Debug($"{(i * 10000 / (float)genResult.size()) / 100f}% done ({i})");
                    nextUpdateTime = DateTime.UtcNow.AddMilliseconds(3000);
                }

            }

        }

        [Test]
        public void sequentialAsksTest()
        {

            //        int hotPricesRange = 1024;
            //        orderBook = new OrderBookFastImpl(hotPricesRange);
            //orderBook = new OrderBookNaiveImpl();

            // empty order book
            clearOrderBook();
            orderBook.validateInternalState();

            // ask prices start from here, overlap with far ask area
            long topPrice = INITIAL_PRICE + 1000;
            // ask prices stop from here, overlap with far bid area
            long bottomPrice = INITIAL_PRICE - 1000;

            int orderId = 100;

            // collecting expected limit order volumes for each price
            Dictionary<long, long> results = new Dictionary<long,long>();

            // placing limit bid orders
            for (long price = bottomPrice; price < INITIAL_PRICE; price++)
            {
                OrderCommand cmd = OrderCommand.newOrder(OrderType.GTC, orderId++, UID_1, price, price * 10, 1, OrderAction.BID);
                //            log.debug("BID {}", price);
                processAndValidate(cmd, CommandResultCode.SUCCESS);
                results[price] = -1L;
            }


            for (long price = topPrice; price >= bottomPrice; price--)
            {
                long size = price * price;
                OrderCommand cmd = OrderCommand.newOrder(OrderType.GTC, orderId++, UID_2, price, 0, size, OrderAction.ASK);
                //            log.debug("ASK {}", price);
                processAndValidate(cmd, CommandResultCode.SUCCESS);

                //results.compute(price, (p, v) => v == null ? size : v + size);
                _ = results.TryGetValue(price, out long tmp);
                results[price] = tmp + size;

                //L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(100000);
                //log.debug("A:{} B:{}", snapshot.askSize, snapshot.bidSize);
            }

            // collecting full order book
            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(int.MaxValue);

            // check the number of records, should match to expected results
            Assert.AreEqual(snapshot.AskSize, results.Count);

            // verify expected size for each price
            for (int i = 0; i < snapshot.AskSize; i++)
            {
                long price = snapshot.AskPrices[i];
                if (!results.TryGetValue(price, out long expectedSize))
                    Assert.Fail();
                //            if (snapshot.askVolumes[i] != expectedSize) {
                //                log.error("volume mismatch for price {} : diff={}", price, snapshot.askVolumes[i] - expectedSize);
                //            }
                Assert.AreEqual(snapshot.AskVolumes[i], expectedSize, "volume mismatch for price " + price);
            }

            // obviously no bid records expected
            Assert.AreEqual(snapshot.BidSize, 0);
        }


        [Test]
        public void sequentialBidsTest()
        {

            // empty order book
            clearOrderBook();
            orderBook.validateInternalState();

            // bid prices starts from here, overlap with far bid area
            long bottomPrice = INITIAL_PRICE - 1000;
            // bid prices stop here, overlap with far ask area
            long topPrice = INITIAL_PRICE + 1000;

            int orderId = 100;

            // collecting expected limit order volumes for each price
            Dictionary<long, long> results = new Dictionary<long,long>();

            // placing limit ask orders
            for (long price = topPrice; price > INITIAL_PRICE; price--)
            {
                OrderCommand cmd = OrderCommand.newOrder(OrderType.GTC, orderId++, TestConstants.UID_1, price, 0, 1, OrderAction.ASK);
                //            log.debug("BID {}", price);
                processAndValidate(cmd, CommandResultCode.SUCCESS);
                results[price] = -1L;
            }

            for (long price = bottomPrice; price <= topPrice; price++)
            {
                long size = price * price;
                OrderCommand cmd = OrderCommand.newOrder(OrderType.GTC, orderId++, UID_2, price, price * 10, size, OrderAction.BID);
                //            log.debug("ASK {}", price);
                processAndValidate(cmd, CommandResultCode.SUCCESS);
                //results.compute(price, (p, v) => v == null ? size : v + size);
                _ = results.TryGetValue(price, out long tmp);
                results[price] = tmp + size;

                //L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(100000);
                //log.debug("A:{} B:{}", snapshot.askSize, snapshot.bidSize);
            }

            // collecting full order book
            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(int.MaxValue);

            // check the number of records, should match to expected results
            Assert.AreEqual(snapshot.BidSize, results.Count);

            // verify expected size for each price
            for (int i = 0; i < snapshot.BidSize; i++)
            {
                long price = snapshot.BidPrices[i];
                if (!results.TryGetValue(price, out long expectedSize))
                    Assert.Fail();
                //            if (snapshot.askVolumes[i] != expectedSize) {
                //                log.error("volume mismatch for price {} : diff={}", price, snapshot.askVolumes[i] - expectedSize);
                //            }
                Assert.AreEqual(snapshot.BidVolumes[i], expectedSize, "volume mismatch for price " + price);
            }

            // obviously no aks records expected (they all should be matched)
            Assert.AreEqual(snapshot.AskSize, 0);
        }


    }
}

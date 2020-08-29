using Exchange.Core.Common;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Orderbook;
using Exchange.Core.Tests.Utils;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Exchange.Core.Tests.Core.OrderBook
{
    /**
 * TODO tests where IOC order is not fully matched because of limit price (similar to OrderType.GTC tests)
 * TODO tests where OrderType.GTC order has duplicate id - rejection event should be sent
 * TODO add tests for exchange mode (moves)
 * TODO test reserve price validation for OrderAction.BID orders in exchange mode
 */
    public abstract class OrderBookBaseTest
    {
        protected IOrderBook orderBook;

        private L2MarketDataHelper expectedState;

        protected static readonly long INITIAL_PRICE = 81600L;
        protected static readonly long MAX_PRICE = 400000L;

        protected static readonly long UID_1 = 412L;
        protected static readonly long UID_2 = 413L;

        protected abstract IOrderBook createNewOrderBook();

        protected abstract CoreSymbolSpecification getCoreSymbolSpec();


        [SetUp]
        public void SetUp()
        {
            orderBook = createNewOrderBook();
            orderBook.validateInternalState();

            processAndValidate(OrderCommand.newOrder(OrderType.GTC, 0L, UID_2, INITIAL_PRICE, 0L, 13L, OrderAction.ASK), CommandResultCode.SUCCESS);
            processAndValidate(OrderCommand.cancel(0L, UID_2), CommandResultCode.SUCCESS);

            processAndValidate(OrderCommand.newOrder(OrderType.GTC, 1L, UID_1, 81600L, 0L, 100L, OrderAction.ASK), CommandResultCode.SUCCESS);
            processAndValidate(OrderCommand.newOrder(OrderType.GTC, 2L, UID_1, 81599L, 0L, 50L, OrderAction.ASK), CommandResultCode.SUCCESS);
            processAndValidate(OrderCommand.newOrder(OrderType.GTC, 3L, UID_1, 81599L, 0L, 25L, OrderAction.ASK), CommandResultCode.SUCCESS);
            processAndValidate(OrderCommand.newOrder(OrderType.GTC, 8L, UID_1, 201000L, 0L, 28L, OrderAction.ASK), CommandResultCode.SUCCESS);
            processAndValidate(OrderCommand.newOrder(OrderType.GTC, 9L, UID_1, 201000L, 0L, 32L, OrderAction.ASK), CommandResultCode.SUCCESS);
            processAndValidate(OrderCommand.newOrder(OrderType.GTC, 10L, UID_1, 200954L, 0L, 10L, OrderAction.ASK), CommandResultCode.SUCCESS);

            processAndValidate(OrderCommand.newOrder(OrderType.GTC, 4L, UID_1, 81593L, 82000L, 40L, OrderAction.BID), CommandResultCode.SUCCESS);
            processAndValidate(OrderCommand.newOrder(OrderType.GTC, 5L, UID_1, 81590L, 82000L, 20L, OrderAction.BID), CommandResultCode.SUCCESS);
            processAndValidate(OrderCommand.newOrder(OrderType.GTC, 6L, UID_1, 81590L, 82000L, 1L, OrderAction.BID), CommandResultCode.SUCCESS);
            processAndValidate(OrderCommand.newOrder(OrderType.GTC, 7L, UID_1, 81200L, 82000L, 20L, OrderAction.BID), CommandResultCode.SUCCESS);
            processAndValidate(OrderCommand.newOrder(OrderType.GTC, 11L, UID_1, 10000L, 12000L, 12L, OrderAction.BID), CommandResultCode.SUCCESS);
            processAndValidate(OrderCommand.newOrder(OrderType.GTC, 12L, UID_1, 10000L, 12000L, 1L, OrderAction.BID), CommandResultCode.SUCCESS);
            processAndValidate(OrderCommand.newOrder(OrderType.GTC, 13L, UID_1, 9136L, 12000L, 2L, OrderAction.BID), CommandResultCode.SUCCESS);

            expectedState = new L2MarketDataHelper(
                    new L2MarketData(
                            new long[] { 81599, 81600, 200954, 201000 },
                            new long[] { 75, 100, 10, 60 },
                            new long[] { 2, 1, 1, 2 },
                            new long[] { 81593, 81590, 81200, 10000, 9136 },
                            new long[] { 40, 21, 20, 13, 2 },
                            new long[] { 1, 2, 1, 2, 1 }
                    )
            );

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(25);
            Assert.AreEqual(expectedState.build(), snapshot);
        }

        /**
         * In the end of each test remove all orders by sending market orders wit proper size.
         * Check order book is empty.
         */
        [TearDown]
        public void TearDown()
        {
            clearOrderBook();
        }

        protected void clearOrderBook()
        {
            orderBook.validateInternalState();
            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(int.MaxValue);

            // match all OrderAction.ASKs
            long askSum = snapshot.AskVolumes.Sum();
            IOrderBook.processCommand(orderBook, OrderCommand.newOrder(OrderType.IOC, 100000000000L, -1, MAX_PRICE, MAX_PRICE, askSum, OrderAction.BID));

            //        log.debug("{}", orderBook.getL2MarketDataSnapshot(int.MaxValue).dumpOrderBook());

            orderBook.validateInternalState();

            // match all OrderAction.BIDs
            long bidSum = snapshot.BidVolumes.Sum();
            IOrderBook.processCommand(orderBook, OrderCommand.newOrder(OrderType.IOC, 100000000001L, -2, 1, 0, bidSum, OrderAction.ASK));

            //        log.debug("{}", orderBook.getL2MarketDataSnapshot(int.MaxValue).dumpOrderBook());

            Assert.AreEqual(orderBook.getL2MarketDataSnapshot(int.MaxValue).AskSize, 0);
            Assert.AreEqual(orderBook.getL2MarketDataSnapshot(int.MaxValue).BidSize, 0);

            orderBook.validateInternalState();
        }


        [Test]
        public void shouldInitializeWithoutErrors()
        {

        }

        // ------------------------ TESTS WITHOUT MATCHING -----------------------

        /**
         * Just place few OrderType.GTC orders
         */
        [Test]
        public void shouldAddGtcOrders()
        {

            IOrderBook.processCommand(orderBook, OrderCommand.newOrder(OrderType.GTC, 93, UID_1, 81598, 0, 1, OrderAction.ASK));
            expectedState.insertAsk(0, 81598, 1);

            IOrderBook.processCommand(orderBook, OrderCommand.newOrder(OrderType.GTC, 94, UID_1, 81594, MAX_PRICE, 9_000_000_000L, OrderAction.BID));
            expectedState.insertBid(0, 81594, 9_000_000_000L);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(25);
            Assert.AreEqual(expectedState.build(), snapshot);
            orderBook.validateInternalState();

            IOrderBook.processCommand(orderBook, OrderCommand.newOrder(OrderType.GTC, 95, UID_1, 130000, 0, 13_000_000_000L, OrderAction.ASK));
            expectedState.insertAsk(3, 130000, 13_000_000_000L);

            IOrderBook.processCommand(orderBook, OrderCommand.newOrder(OrderType.GTC, 96, UID_1, 1000, MAX_PRICE, 4, OrderAction.BID));
            expectedState.insertBid(6, 1000, 4);

            snapshot = orderBook.getL2MarketDataSnapshot(25);
            Assert.AreEqual(expectedState.build(), snapshot);
            orderBook.validateInternalState();

            //        log.debug("{}", dumpOrderBook(snapshot));
        }

        /**
         * Ignore order with duplicate orderId
         */
        [Test]
        public void shouldIgnoredDuplicateOrder()
        {
            OrderCommand orderCommand = OrderCommand.newOrder(OrderType.GTC, 1, UID_1, 81600, 0, 100, OrderAction.ASK);
            processAndValidate(orderCommand, CommandResultCode.SUCCESS);
            List<MatcherTradeEvent> events = orderCommand.extractEvents();
            Assert.AreEqual(events.Count, 1);
        }

        /**
         * Remove existing order
         */
        [Test]
        public void shouldRemoveBidOrder()
        {

            // remove OrderAction.BID order
            OrderCommand cmd = OrderCommand.cancel(5, UID_1);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            expectedState.setBidVolume(1, 1).decrementBidOrdersNum(1);
            Assert.AreEqual(expectedState.build(), orderBook.getL2MarketDataSnapshot(25));

            Assert.AreEqual(cmd.Action, OrderAction.BID);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 1);
            checkEventReduce(events[0], 20L, 81590, true, default);
        }

        [Test]
        public void shouldRemoveAskOrder()
        {
            // remove OrderAction.ASK order
            OrderCommand cmd = OrderCommand.cancel(2, UID_1);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            expectedState.setAskVolume(0, 25).decrementAskOrdersNum(0);
            Assert.AreEqual(expectedState.build(), orderBook.getL2MarketDataSnapshot(25));

            Assert.AreEqual(cmd.Action, OrderAction.ASK);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 1);
            checkEventReduce(events[0], 50L, 81599L, true, null);
        }

        [Test]
        public void shouldReduceBidOrder()
        {

            // reduce OrderAction.BID order
            OrderCommand cmd = OrderCommand.reduce(5, UID_1, 3);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            expectedState.decrementBidVolume(1, 3);
            Assert.AreEqual(expectedState.build(), orderBook.getL2MarketDataSnapshot());

            Assert.AreEqual(cmd.Action, OrderAction.BID);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 1);
            checkEventReduce(events[0], 3L, 81590L, false, null);
        }

        [Test]
        public void shouldReduceAskOrder()
        {
            // reduce OrderAction.ASK order - will effectively remove order
            OrderCommand cmd = OrderCommand.reduce(1, UID_1, 300);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            expectedState.removeAsk(1);
            Assert.AreEqual(expectedState.build(), orderBook.getL2MarketDataSnapshot());

            Assert.AreEqual(cmd.Action, OrderAction.ASK);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 1);
            checkEventReduce(events[0], 100L, 81600L, true, null);
        }

        /**
         * When cancelling an order, order book implementation should also remove a bucket if no orders left for specified price
         */
        [Test]
        public void shouldRemoveOrderAndEmptyBucket()
        {
            OrderCommand cmdCancel2 = OrderCommand.cancel(2, UID_1);
            processAndValidate(cmdCancel2, CommandResultCode.SUCCESS);

            Assert.AreEqual(cmdCancel2.Action, OrderAction.ASK);

            List<MatcherTradeEvent> events = cmdCancel2.extractEvents();
            Assert.AreEqual(events.Count, 1);
            checkEventReduce(events[0], 50L, 81599L, true, null);

            //log.debug("{}", orderBook.getL2MarketDataSnapshot(10).dumpOrderBook());

            OrderCommand cmdCancel3 = OrderCommand.cancel(3, UID_1);
            processAndValidate(cmdCancel3, CommandResultCode.SUCCESS);

            Assert.AreEqual(cmdCancel3.Action, OrderAction.ASK);

            Assert.AreEqual(expectedState.removeAsk(0).build(), orderBook.getL2MarketDataSnapshot());

            events = cmdCancel3.extractEvents();
            Assert.AreEqual(events.Count, 1);
            checkEventReduce(events[0], 25L, 81599L, true, null);
        }

        [Test]
        public void shouldReturnErrorWhenDeletingUnknownOrder()
        {

            OrderCommand cmd = OrderCommand.cancel(5291, UID_1);
            processAndValidate(cmd, CommandResultCode.MATCHING_UNKNOWN_ORDER_ID);

            Assert.AreEqual(expectedState.build(), orderBook.getL2MarketDataSnapshot());

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 0);
        }

        [Test]
        public void shouldReturnErrorWhenDeletingOtherUserOrder()
        {
            OrderCommand cmd = OrderCommand.cancel(3, UID_2);
            processAndValidate(cmd, CommandResultCode.MATCHING_UNKNOWN_ORDER_ID);
            Assert.IsNull(cmd.MatcherEvent);

            Assert.AreEqual(expectedState.build(), orderBook.getL2MarketDataSnapshot());
        }

        [Test]
        public void shouldReturnErrorWhenUpdatingOtherUserOrder()
        {
            OrderCommand cmd = OrderCommand.update(2, UID_2, 100);
            processAndValidate(cmd, CommandResultCode.MATCHING_UNKNOWN_ORDER_ID);
            Assert.IsNull(cmd.MatcherEvent);

            cmd = OrderCommand.update(8, UID_2, 100);
            processAndValidate(cmd, CommandResultCode.MATCHING_UNKNOWN_ORDER_ID);
            Assert.IsNull(cmd.MatcherEvent);

            Assert.AreEqual(expectedState.build(), orderBook.getL2MarketDataSnapshot());
        }

        [Test]
        public void shouldReturnErrorWhenUpdatingUnknownOrder()
        {

            OrderCommand cmd = OrderCommand.update(2433, UID_1, 300);
            processAndValidate(cmd, CommandResultCode.MATCHING_UNKNOWN_ORDER_ID);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
            //        log.debug("{}", dumpOrderBook(snapshot));

            Assert.AreEqual(expectedState.build(), snapshot);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 0);
        }

        [Test]
        public void shouldReturnErrorWhenReducingUnknownOrder()
        {

            OrderCommand cmd = OrderCommand.reduce(3, UID_2, 1);
            processAndValidate(cmd, CommandResultCode.MATCHING_UNKNOWN_ORDER_ID);
            Assert.IsNull(cmd.MatcherEvent);

            Assert.AreEqual(expectedState.build(), orderBook.getL2MarketDataSnapshot());
        }

        [Test]
        public void shouldReturnErrorWhenReducingByZeroOrNegativeSize()
        {

            OrderCommand cmd = OrderCommand.reduce(4, UID_1, 0);
            processAndValidate(cmd, CommandResultCode.MATCHING_REDUCE_FAILED_WRONG_SIZE);
            Assert.IsNull(cmd.MatcherEvent);

            cmd = OrderCommand.reduce(8, UID_1, -1);
            processAndValidate(cmd, CommandResultCode.MATCHING_REDUCE_FAILED_WRONG_SIZE);
            Assert.IsNull(cmd.MatcherEvent);

            cmd = OrderCommand.reduce(8, UID_1, long.MinValue);
            processAndValidate(cmd, CommandResultCode.MATCHING_REDUCE_FAILED_WRONG_SIZE);
            Assert.IsNull(cmd.MatcherEvent);

            Assert.AreEqual(expectedState.build(), orderBook.getL2MarketDataSnapshot());
        }

        [Test]
        public void shouldReturnErrorWhenReducingOtherUserOrder()
        {

            OrderCommand cmd = OrderCommand.reduce(8, UID_2, 3);
            processAndValidate(cmd, CommandResultCode.MATCHING_UNKNOWN_ORDER_ID);
            Assert.IsNull(cmd.MatcherEvent);

            Assert.AreEqual(expectedState.build(), orderBook.getL2MarketDataSnapshot());
        }

        [Test]
        public void shouldMoveOrderExistingBucket()
        {
            OrderCommand cmd = OrderCommand.update(7, UID_1, 81590);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);

            // moved
            L2MarketData expected = expectedState.setBidVolume(1, 41).incrementBidOrdersNum(1).removeBid(2).build();
            Assert.AreEqual(expected, snapshot);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 0);
        }

        [Test]
        public void shouldMoveOrderNewBucket()
        {
            OrderCommand cmd = OrderCommand.update(7, UID_1, 81594);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);

            // moved
            L2MarketData expected = expectedState.removeBid(2).insertBid(0, 81594, 20).build();
            Assert.AreEqual(expected, snapshot);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 0);
        }

        // ------------------------ MATCHING TESTS -----------------------

        [Test]
        public void shouldMatchIocOrderPartialBBO()
        {

            // size=10
            OrderCommand cmd = OrderCommand.newOrder(OrderType.IOC, 123, UID_2, 1, 0, 10, OrderAction.ASK);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
            // best OrderAction.BID matched
            L2MarketData expected = expectedState.setBidVolume(0, 30).build();
            Assert.AreEqual(expected, snapshot);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 1);
            checkEventTrade(events[0], 4L, 81593, 10L);
        }


        [Test]
        public void shouldMatchIocOrderFullBBO()
        {

            // size=40
            OrderCommand cmd = OrderCommand.newOrder(OrderType.IOC, 123, UID_2, 1, 0, 40, OrderAction.ASK);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
            // best OrderAction.BID matched
            L2MarketData expected = expectedState.removeBid(0).build();
            Assert.AreEqual(expected, snapshot);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 1);
            checkEventTrade(events[0], 4L, 81593, 40L);
        }

        [Test]
        public void shouldMatchIocOrderWithTwoLimitOrdersPartial()
        {

            // size=41
            OrderCommand cmd = OrderCommand.newOrder(OrderType.IOC, 123, UID_2, 1, 0, 41, OrderAction.ASK);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
            // OrderAction.BIDs matched
            L2MarketData expected = expectedState.removeBid(0).setBidVolume(0, 20).build();
            Assert.AreEqual(expected, snapshot);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 2);
            checkEventTrade(events[0], 4L, 81593, 40L);
            checkEventTrade(events[1], 5L, 81590, 1L);

            // check orders are removed from map
            Assert.IsNull(orderBook.getOrderById(4L));
            Assert.NotNull(orderBook.getOrderById(5L));
        }


        [Test]
        public void shouldMatchIocOrderFullLiquidity()
        {

            // size=175
            OrderCommand cmd = OrderCommand.newOrder(OrderType.IOC, 123, UID_2, MAX_PRICE, MAX_PRICE, 175, OrderAction.BID);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
            // all OrderAction.ASKs matched
            L2MarketData expected = expectedState.removeAsk(0).removeAsk(0).build();
            Assert.AreEqual(expected, snapshot);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 3);
            checkEventTrade(events[0], 2L, 81599L, 50L);
            checkEventTrade(events[1], 3L, 81599L, 25L);
            checkEventTrade(events[2], 1L, 81600L, 100L);

            // check orders are removed from map
            Assert.IsNull(orderBook.getOrderById(1L));
            Assert.IsNull(orderBook.getOrderById(2L));
            Assert.IsNull(orderBook.getOrderById(3L));
        }

        [Test]
        public void shouldMatchIocOrderWithRejection()
        {

            // size=270
            OrderCommand cmd = OrderCommand.newOrder(OrderType.IOC, 123, UID_2, MAX_PRICE, MAX_PRICE + 1, 270, OrderAction.BID);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
            // all OrderAction.ASKs matched
            L2MarketData expected = expectedState.removeAllAsks().build();
            Assert.AreEqual(expected, snapshot);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 7);

            // 6 trades generated, first comes rejection with size=25 left unmatched
            checkEventRejection(events[0], 25L, 400000L, MAX_PRICE + 1);
        }

        // ---------------------- FOK BUDGET ORDERS ---------------------------

        [Test]
        public void shouldRejectFokBidOrderOutOfBudget()
        {

            long size = 180L;
            long buyBudget = expectedState.aggregateBuyBudget(size) - 1;
            Assert.AreEqual(buyBudget, 81599L * 75L + 81600L * 100L + 200954L * 5L - 1);

            OrderCommand cmd = OrderCommand.newOrder(OrderType.FOK_BUDGET, 123L, UID_2, buyBudget, buyBudget, size, OrderAction.BID);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
            Assert.AreEqual(expectedState.build(), snapshot);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 1);

            // no trades generated, rejection with full size unmatched
            checkEventRejection(events[0], size, buyBudget, buyBudget);
        }

        [Test]
        public void shouldMatchFokBidOrderExactBudget()
        {

            long size = 180L;
            long buyBudget = expectedState.aggregateBuyBudget(size);
            Assert.AreEqual(buyBudget, 81599L * 75L + 81600L * 100L + 200954L * 5L);

            OrderCommand cmd = OrderCommand.newOrder(OrderType.FOK_BUDGET, 123L, UID_2, buyBudget, buyBudget, size, OrderAction.BID);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
            Assert.AreEqual(expectedState.removeAsk(0).removeAsk(0).setAskVolume(0, 5).build(), snapshot);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 4);
            checkEventTrade(events[0], 2L, 81599, 50L);
            checkEventTrade(events[1], 3L, 81599, 25L);
            checkEventTrade(events[2], 1L, 81600L, 100L);
            checkEventTrade(events[3], 10L, 200954L, 5L);
        }

        [Test]
        public void shouldMatchFokBidOrderExtraBudget()
        {

            long size = 176L;
            long buyBudget = expectedState.aggregateBuyBudget(size) + 1;
            Assert.AreEqual(buyBudget, 81599L * 75L + 81600L * 100L + 200954L + 1L);

            OrderCommand cmd = OrderCommand.newOrder(OrderType.FOK_BUDGET, 123L, UID_2, buyBudget, buyBudget, size, OrderAction.BID);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
            Assert.AreEqual(expectedState.removeAsk(0).removeAsk(0).setAskVolume(0, 9).build(), snapshot);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 4);
            checkEventTrade(events[0], 2L, 81599, 50L);
            checkEventTrade(events[1], 3L, 81599, 25L);
            checkEventTrade(events[2], 1L, 81600L, 100L);
            checkEventTrade(events[3], 10L, 200954L, 1L);
        }

        [Test]
        public void shouldRejectFokAskOrderBelowExpectation()
        {

            long size = 60L;
            long sellExpectation = expectedState.aggregateSellExpectation(size) + 1;
            Assert.AreEqual(sellExpectation, 81593L * 40L + 81590L * 20L + 1);

            OrderCommand cmd = OrderCommand.newOrder(OrderType.FOK_BUDGET, 123L, UID_2, sellExpectation, sellExpectation, size, OrderAction.ASK);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
            Assert.AreEqual(expectedState.build(), snapshot);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 1);
            // no trades generated, rejection with full size unmatched
            checkEventRejection(events[0], size, sellExpectation, sellExpectation);
        }

        [Test]
        public void shouldMatchFokAskOrderExactExpectation()
        {

            long size = 60L;
            long sellExpectation = expectedState.aggregateSellExpectation(size);
            Assert.AreEqual(sellExpectation, 81593L * 40L + 81590L * 20L);

            OrderCommand cmd = OrderCommand.newOrder(OrderType.FOK_BUDGET, 123L, UID_2, sellExpectation, sellExpectation, size, OrderAction.ASK);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
            Assert.AreEqual(expectedState.removeBid(0).setBidVolume(0, 1).decrementBidOrdersNum(0).build(), snapshot);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 2);
            checkEventTrade(events[0], 4L, 81593L, 40L);
            checkEventTrade(events[1], 5L, 81590L, 20L);
        }

        [Test]
        public void shouldMatchFokAskOrderExtraBudget()
        {

            long size = 61L;
            long sellExpectation = expectedState.aggregateSellExpectation(size) - 1;
            Assert.AreEqual(sellExpectation, 81593L * 40L + 81590L * 21L - 1);

            OrderCommand cmd = OrderCommand.newOrder(OrderType.FOK_BUDGET, 123L, UID_2, sellExpectation, sellExpectation, size, OrderAction.ASK);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
            Assert.AreEqual(expectedState.removeBid(0).removeBid(0).build(), snapshot);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 3);
            checkEventTrade(events[0], 4L, 81593L, 40L);
            checkEventTrade(events[1], 5L, 81590L, 20L);
            checkEventTrade(events[2], 6L, 81590L, 1L);
        }


        // MARKETABLE OrderType.GTC ORDERS

        [Test]
        public void shouldFullyMatchMarketableGtcOrder()
        {

            // size=1
            OrderCommand cmd = OrderCommand.newOrder(OrderType.GTC, 123, UID_2, 81599, MAX_PRICE, 1, OrderAction.BID);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
            // best OrderAction.ASK partially matched
            L2MarketData expected = expectedState.setAskVolume(0, 74).build();
            Assert.AreEqual(expected, snapshot);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 1);
            checkEventTrade(events[0], 2L, 81599, 1L);
        }


        [Test]
        public void shouldPartiallyMatchMarketableGtcOrderAndPlace()
        {

            // size=77
            OrderCommand cmd = OrderCommand.newOrder(OrderType.GTC, 123, UID_2, 81599, MAX_PRICE, 77, OrderAction.BID);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
            // best OrderAction.ASKs fully matched, limit OrderAction.BID order placed
            L2MarketData expected = expectedState.removeAsk(0).insertBid(0, 81599, 2).build();
            Assert.AreEqual(expected, snapshot);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 2);

            checkEventTrade(events[0], 2L, 81599, 50L);
            checkEventTrade(events[1], 3L, 81599, 25L);
        }

        [Test]
        public void shouldFullyMatchMarketableGtcOrder2Prices()
        {

            // size=77
            OrderCommand cmd = OrderCommand.newOrder(OrderType.GTC, 123, UID_2, 81600, MAX_PRICE, 77, OrderAction.BID);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
            // best OrderAction.ASKs fully matched, limit OrderAction.BID order placed
            L2MarketData expected = expectedState.removeAsk(0).setAskVolume(0, 98).build();
            Assert.AreEqual(expected, snapshot);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 3);

            checkEventTrade(events[0], 2L, 81599, 50L);
            checkEventTrade(events[1], 3L, 81599, 25L);
            checkEventTrade(events[2], 1L, 81600, 2L);
        }


        [Test]
        public void shouldFullyMatchMarketableGtcOrderWithAllLiquidity()
        {

            // size=1000
            OrderCommand cmd = OrderCommand.newOrder(OrderType.GTC, 123, UID_2, 220000, MAX_PRICE, 1000, OrderAction.BID);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
            // best OrderAction.ASKs fully matched, limit OrderAction.BID order placed
            L2MarketData expected = expectedState.removeAllAsks().insertBid(0, 220000, 755).build();
            Assert.AreEqual(expected, snapshot);

            // trades only, rejection not generated for limit order
            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 6);

            checkEventTrade(events[0], 2L, 81599, 50L);
            checkEventTrade(events[1], 3L, 81599, 25L);
            checkEventTrade(events[2], 1L, 81600, 100L);
            checkEventTrade(events[3], 10L, 200954, 10L);
            checkEventTrade(events[4], 8L, 201000, 28L);
            checkEventTrade(events[5], 9L, 201000, 32L);
        }


        // Move OrderType.GTC order to marketable price
        // TODO add into far area
        [Test]
        public void shouldMoveOrderFullyMatchAsMarketable()
        {

            // add new order and check it is there
            OrderCommand cmd = OrderCommand.newOrder(OrderType.GTC, 83, UID_2, 81200, MAX_PRICE, 20, OrderAction.BID);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 0);

            L2MarketData expected = expectedState.setBidVolume(2, 40).incrementBidOrdersNum(2).build();
            Assert.AreEqual(expected, orderBook.getL2MarketDataSnapshot(10));

            // move to marketable price area
            cmd = OrderCommand.update(83, UID_2, 81602);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            // moved
            expected = expectedState.setBidVolume(2, 20).decrementBidOrdersNum(2).setAskVolume(0, 55).build();
            Assert.AreEqual(expected, orderBook.getL2MarketDataSnapshot(10));

            events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 1);
            checkEventTrade(events[0], 2L, 81599, 20L);
        }


        [Test]
        public void shouldMoveOrderFullyMatchAsMarketable2Prices()
        {

            OrderCommand cmd = OrderCommand.newOrder(OrderType.GTC, 83, UID_2, 81594, MAX_PRICE, 100, OrderAction.BID);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 0);

            // move to marketable zone
            cmd = OrderCommand.update(83, UID_2, 81600);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);

            // moved
            L2MarketData expected = expectedState.removeAsk(0).setAskVolume(0, 75).build();
            Assert.AreEqual(expected, snapshot);

            events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 3);
            checkEventTrade(events[0], 2L, 81599, 50L);
            checkEventTrade(events[1], 3L, 81599, 25L);
            checkEventTrade(events[2], 1L, 81600, 25L);

        }

        [Test]
        public void shouldMoveOrderMatchesAllLiquidity()
        {

            OrderCommand cmd = OrderCommand.newOrder(OrderType.GTC, 83, UID_2, 81594, MAX_PRICE, 246, OrderAction.BID);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            // move to marketable zone
            cmd = OrderCommand.update(83, UID_2, 201000);
            processAndValidate(cmd, CommandResultCode.SUCCESS);

            L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);

            // moved
            L2MarketData expected = expectedState.removeAllAsks().insertBid(0, 201000, 1).build();
            Assert.AreEqual(expected, snapshot);

            List<MatcherTradeEvent> events = cmd.extractEvents();
            Assert.AreEqual(events.Count, 6);
            checkEventTrade(events[0], 2L, 81599, 50L);
            checkEventTrade(events[1], 3L, 81599, 25L);
            checkEventTrade(events[2], 1L, 81600, 100L);
            checkEventTrade(events[3], 10L, 200954, 10L);
            checkEventTrade(events[4], 8L, 201000, 28L);
            checkEventTrade(events[5], 9L, 201000, 32L);
        }


        [Test]
        public void multipleCommandsKeepInternalStateTest()
        {

            int tranNum = 25000;

            IOrderBook localOrderBook = createNewOrderBook();
            localOrderBook.validateInternalState();

            GenResult genResult = TestOrdersGenerator.generateCommands(
                    tranNum,
                    200,
                    6,
                    TestOrdersGenerator.UID_PLAIN_MAPPER,
                    0,
                    false,
                    false,
                    TestOrdersGenerator.createAsyncProgressLogger(tranNum),
                    348290254);

            foreach (var cmd in genResult.getCommands())
            {
                cmd.OrderId += 100; // TODO set start id
                                    //log.debug("{}",  cmd);
                CommandResultCode commandResultCode = IOrderBook.processCommand(localOrderBook, cmd);
                Assert.AreEqual(commandResultCode, CommandResultCode.SUCCESS);
                localOrderBook.validateInternalState();
            }
        }

        // ------------------------------- UTILITY METHODS --------------------------

        public void processAndValidate(OrderCommand cmd, CommandResultCode expectedCmdState)
        {
            CommandResultCode resultCode = IOrderBook.processCommand(orderBook, cmd);
            Assert.AreEqual(resultCode, expectedCmdState);
            orderBook.validateInternalState();
        }

        public void checkEventTrade(MatcherTradeEvent evnt, long matchedId, long price, long size)
        {
            Assert.AreEqual(evnt.EventType, MatcherEventType.TRADE);
            Assert.AreEqual(evnt.MatchedOrderId, matchedId);
            Assert.AreEqual(evnt.Price, price);
            Assert.AreEqual(evnt.Size, size);
            // TODO add more checks for MatcherTradeEvent
        }

        public void checkEventRejection(MatcherTradeEvent evnt, long size, long price, long? bidderHoldPrice)
        {
            Assert.AreEqual(evnt.EventType, MatcherEventType.REJECT);
            Assert.AreEqual(evnt.Size, size);
            Assert.AreEqual(evnt.Price, price);
            Assert.AreEqual(evnt.ActiveOrderCompleted, true);
            if (bidderHoldPrice.HasValue)
            {
                Assert.AreEqual(evnt.BidderHoldPrice, bidderHoldPrice);
            }
        }

        public void checkEventReduce(MatcherTradeEvent evnt, long reduceSize, long price, bool completed, long? bidderHoldPrice)
        {
            Assert.AreEqual(evnt.EventType, MatcherEventType.REDUCE);
            Assert.AreEqual(evnt.Size, reduceSize);
            Assert.AreEqual(evnt.Price, price);
            Assert.AreEqual(evnt.ActiveOrderCompleted, completed);
            Assert.IsNull(evnt.NextEvent);
            if (bidderHoldPrice.HasValue)
            {
                Assert.AreEqual(evnt.BidderHoldPrice, bidderHoldPrice);
            }
        }

    }
}

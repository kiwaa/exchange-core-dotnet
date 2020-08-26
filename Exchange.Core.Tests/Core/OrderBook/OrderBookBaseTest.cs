using Exchange.Core.Common;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Orderbook;
using NUnit.Framework;
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
        IOrderBook orderBook;

        private L2MarketDataHelper expectedState;

        static readonly long INITIAL_PRICE = 81600L;
        static readonly long MAX_PRICE = 400000L;

        static readonly long UID_1 = 412L;
        static readonly long UID_2 = 413L;

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

        void clearOrderBook()
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

        //    // ------------------------ TESTS WITHOUT MATCHING -----------------------

        //    /**
        //     * Just place few OrderType.GTC orders
        //     */
        //    @Test
        //public void shouldAddOrderType.GTCOrders()
        //    {

        //        IOrderBook.processCommand(orderBook, OrderCommand.newOrder(OrderType.GTC, 93, UID_1, 81598, 0, 1, OrderAction.ASK));
        //        expectedState.insertOrderAction.ASK(0, 81598, 1);

        //        IOrderBook.processCommand(orderBook, OrderCommand.newOrder(OrderType.GTC, 94, UID_1, 81594, MAX_PRICE, 9_000_000_000L, OrderAction.BID));
        //        expectedState.insertOrderAction.BID(0, 81594, 9_000_000_000L);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(25);
        //        assertEquals(expectedState.build(), snapshot);
        //        orderBook.validateInternalState();

        //        IOrderBook.processCommand(orderBook, OrderCommand.newOrder(OrderType.GTC, 95, UID_1, 130000, 0, 13_000_000_000L, OrderAction.ASK));
        //        expectedState.insertOrderAction.ASK(3, 130000, 13_000_000_000L);

        //        IOrderBook.processCommand(orderBook, OrderCommand.newOrder(OrderType.GTC, 96, UID_1, 1000, MAX_PRICE, 4, OrderAction.BID));
        //        expectedState.insertOrderAction.BID(6, 1000, 4);

        //        snapshot = orderBook.getL2MarketDataSnapshot(25);
        //        assertEquals(expectedState.build(), snapshot);
        //        orderBook.validateInternalState();

        //        //        log.debug("{}", dumpOrderBook(snapshot));
        //    }

        //    /**
        //     * Ignore order with duplicate orderId
        //     */
        //    @Test
        //public void shouldIgnoredDuplicateOrder()
        //    {
        //        OrderCommand orderCommand = OrderCommand.newOrder(OrderType.GTC, 1, UID_1, 81600, 0, 100, OrderAction.ASK);
        //        processAndValidate(orderCommand, CommandResultCode.SUCCESS);
        //        List<MatcherTradeEvent> events = orderCommand.extractEvents();
        //        assertThat(events.size(), is (1));
        //    }

        //    /**
        //     * Remove existing order
        //     */
        //    @Test
        //public void shouldRemoveOrderAction.BIDOrder()
        //    {

        //        // remove OrderAction.BID order
        //        OrderCommand cmd = OrderCommand.cancel(5, UID_1);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        expectedState.setOrderAction.BIDVolume(1, 1).decrementOrderAction.BIDOrdersNum(1);
        //        assertEquals(expectedState.build(), orderBook.getL2MarketDataSnapshot(25));

        //        assertThat(cmd.action, is (OrderAction.BID));

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (1));
        //        checkEventReduce(events.get(0), 20L, 81590, true, null);
        //    }

        //    @Test
        //public void shouldRemoveOrderAction.ASKOrder()
        //    {
        //        // remove OrderAction.ASK order
        //        OrderCommand cmd = OrderCommand.cancel(2, UID_1);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        expectedState.setOrderAction.ASKVolume(0, 25).decrementOrderAction.ASKOrdersNum(0);
        //        assertEquals(expectedState.build(), orderBook.getL2MarketDataSnapshot(25));

        //        assertThat(cmd.action, is (OrderAction.ASK));

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (1));
        //        checkEventReduce(events.get(0), 50L, 81599L, true, null);
        //    }

        //    @Test
        //public void shouldReduceOrderAction.BIDOrder()
        //    {

        //        // reduce OrderAction.BID order
        //        OrderCommand cmd = OrderCommand.reduce(5, UID_1, 3);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        expectedState.decrementOrderAction.BIDVolume(1, 3);
        //        assertEquals(expectedState.build(), orderBook.getL2MarketDataSnapshot());

        //        assertThat(cmd.action, is (OrderAction.BID));

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (1));
        //        checkEventReduce(events.get(0), 3L, 81590L, false, null);
        //    }

        //    @Test
        //public void shouldReduceOrderAction.ASKOrder()
        //    {
        //        // reduce OrderAction.ASK order - will effectively remove order
        //        OrderCommand cmd = OrderCommand.reduce(1, UID_1, 300);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        expectedState.removeOrderAction.ASK(1);
        //        assertEquals(expectedState.build(), orderBook.getL2MarketDataSnapshot());

        //        assertThat(cmd.action, is (OrderAction.ASK));

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (1));
        //        checkEventReduce(events.get(0), 100L, 81600L, true, null);
        //    }

        //    /**
        //     * When cancelling an order, order book implementation should also remove a bucket if no orders left for specified price
        //     */
        //    @Test
        //public void shouldRemoveOrderAndEmptyBucket()
        //    {
        //        OrderCommand cmdCancel2 = OrderCommand.cancel(2, UID_1);
        //        processAndValidate(cmdCancel2, CommandResultCode.SUCCESS);

        //        assertThat(cmdCancel2.action, is (OrderAction.ASK));

        //        List<MatcherTradeEvent> events = cmdCancel2.extractEvents();
        //        assertThat(events.size(), is (1));
        //        checkEventReduce(events.get(0), 50L, 81599L, true, null);

        //        //log.debug("{}", orderBook.getL2MarketDataSnapshot(10).dumpOrderBook());

        //        OrderCommand cmdCancel3 = OrderCommand.cancel(3, UID_1);
        //        processAndValidate(cmdCancel3, CommandResultCode.SUCCESS);

        //        assertThat(cmdCancel3.action, is (OrderAction.ASK));

        //        assertEquals(expectedState.removeOrderAction.ASK(0).build(), orderBook.getL2MarketDataSnapshot());

        //        events = cmdCancel3.extractEvents();
        //        assertThat(events.size(), is (1));
        //        checkEventReduce(events.get(0), 25L, 81599L, true, null);
        //    }

        //    @Test
        //public void shouldReturnErrorWhenDeletingUnknownOrder()
        //    {

        //        OrderCommand cmd = OrderCommand.cancel(5291, UID_1);
        //        processAndValidate(cmd, MATCHING_UNKNOWN_ORDER_ID);

        //        assertEquals(expectedState.build(), orderBook.getL2MarketDataSnapshot());

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (0));
        //    }

        //    @Test
        //public void shouldReturnErrorWhenDeletingOtherUserOrder()
        //    {
        //        OrderCommand cmd = OrderCommand.cancel(3, UID_2);
        //        processAndValidate(cmd, MATCHING_UNKNOWN_ORDER_ID);
        //        assertNull(cmd.matcherEvent);

        //        assertEquals(expectedState.build(), orderBook.getL2MarketDataSnapshot());
        //    }

        //    @Test
        //public void shouldReturnErrorWhenUpdatingOtherUserOrder()
        //    {
        //        OrderCommand cmd = OrderCommand.update(2, UID_2, 100);
        //        processAndValidate(cmd, MATCHING_UNKNOWN_ORDER_ID);
        //        assertNull(cmd.matcherEvent);

        //        cmd = OrderCommand.update(8, UID_2, 100);
        //        processAndValidate(cmd, MATCHING_UNKNOWN_ORDER_ID);
        //        assertNull(cmd.matcherEvent);

        //        assertEquals(expectedState.build(), orderBook.getL2MarketDataSnapshot());
        //    }

        //    @Test
        //public void shouldReturnErrorWhenUpdatingUnknownOrder()
        //    {

        //        OrderCommand cmd = OrderCommand.update(2433, UID_1, 300);
        //        processAndValidate(cmd, MATCHING_UNKNOWN_ORDER_ID);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
        //        //        log.debug("{}", dumpOrderBook(snapshot));

        //        assertEquals(expectedState.build(), snapshot);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (0));
        //    }

        //    @Test
        //public void shouldReturnErrorWhenReducingUnknownOrder()
        //    {

        //        OrderCommand cmd = OrderCommand.reduce(3, UID_2, 1);
        //        processAndValidate(cmd, MATCHING_UNKNOWN_ORDER_ID);
        //        assertNull(cmd.matcherEvent);

        //        assertEquals(expectedState.build(), orderBook.getL2MarketDataSnapshot());
        //    }

        //    @Test
        //public void shouldReturnErrorWhenReducingByZeroOrNegativeSize()
        //    {

        //        OrderCommand cmd = OrderCommand.reduce(4, UID_1, 0);
        //        processAndValidate(cmd, MATCHING_REDUCE_FAILED_WRONG_SIZE);
        //        assertNull(cmd.matcherEvent);

        //        cmd = OrderCommand.reduce(8, UID_1, -1);
        //        processAndValidate(cmd, MATCHING_REDUCE_FAILED_WRONG_SIZE);
        //        assertNull(cmd.matcherEvent);

        //        cmd = OrderCommand.reduce(8, UID_1, Long.MIN_VALUE);
        //        processAndValidate(cmd, MATCHING_REDUCE_FAILED_WRONG_SIZE);
        //        assertNull(cmd.matcherEvent);

        //        assertEquals(expectedState.build(), orderBook.getL2MarketDataSnapshot());
        //    }

        //    @Test
        //public void shouldReturnErrorWhenReducingOtherUserOrder()
        //    {

        //        OrderCommand cmd = OrderCommand.reduce(8, UID_2, 3);
        //        processAndValidate(cmd, MATCHING_UNKNOWN_ORDER_ID);
        //        assertNull(cmd.matcherEvent);

        //        assertEquals(expectedState.build(), orderBook.getL2MarketDataSnapshot());
        //    }

        //    @Test
        //public void shouldMoveOrderExistingBucket()
        //    {
        //        OrderCommand cmd = OrderCommand.update(7, UID_1, 81590);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);

        //        // moved
        //        L2MarketData expected = expectedState.setOrderAction.BIDVolume(1, 41).incrementOrderAction.BIDOrdersNum(1).removeOrderAction.BID(2).build();
        //        assertEquals(expected, snapshot);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (0));
        //    }

        //    @Test
        //public void shouldMoveOrderNewBucket()
        //    {
        //        OrderCommand cmd = OrderCommand.update(7, UID_1, 81594);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);

        //        // moved
        //        L2MarketData expected = expectedState.removeOrderAction.BID(2).insertOrderAction.BID(0, 81594, 20).build();
        //        assertEquals(expected, snapshot);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (0));
        //    }

        //    // ------------------------ MATCHING TESTS -----------------------

        //    @Test
        //public void shouldMatchIocOrderPartialBBO()
        //    {

        //        // size=10
        //        OrderCommand cmd = OrderCommand.newOrder(IOC, 123, UID_2, 1, 0, 10, OrderAction.ASK);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
        //        // best OrderAction.BID matched
        //        L2MarketData expected = expectedState.setOrderAction.BIDVolume(0, 30).build();
        //        assertEquals(expected, snapshot);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (1));
        //        checkEventTrade(events.get(0), 4L, 81593, 10L);
        //    }


        //    @Test
        //public void shouldMatchIocOrderFullBBO()
        //    {

        //        // size=40
        //        OrderCommand cmd = OrderCommand.newOrder(IOC, 123, UID_2, 1, 0, 40, OrderAction.ASK);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
        //        // best OrderAction.BID matched
        //        L2MarketData expected = expectedState.removeOrderAction.BID(0).build();
        //        assertEquals(expected, snapshot);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (1));
        //        checkEventTrade(events.get(0), 4L, 81593, 40L);
        //    }

        //    @Test
        //public void shouldMatchIocOrderWithTwoLimitOrdersPartial()
        //    {

        //        // size=41
        //        OrderCommand cmd = OrderCommand.newOrder(IOC, 123, UID_2, 1, 0, 41, OrderAction.ASK);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
        //        // OrderAction.BIDs matched
        //        L2MarketData expected = expectedState.removeOrderAction.BID(0).setOrderAction.BIDVolume(0, 20).build();
        //        assertEquals(expected, snapshot);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (2));
        //        checkEventTrade(events.get(0), 4L, 81593, 40L);
        //        checkEventTrade(events.get(1), 5L, 81590, 1L);

        //        // check orders are removed from map
        //        assertNull(orderBook.getOrderById(4L));
        //        assertNotNull(orderBook.getOrderById(5L));
        //    }


        //    @Test
        //public void shouldMatchIocOrderFullLiquidity()
        //    {

        //        // size=175
        //        OrderCommand cmd = OrderCommand.newOrder(IOC, 123, UID_2, MAX_PRICE, MAX_PRICE, 175, OrderAction.BID);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
        //        // all OrderAction.ASKs matched
        //        L2MarketData expected = expectedState.removeOrderAction.ASK(0).removeOrderAction.ASK(0).build();
        //        assertEquals(expected, snapshot);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (3));
        //        checkEventTrade(events.get(0), 2L, 81599L, 50L);
        //        checkEventTrade(events.get(1), 3L, 81599L, 25L);
        //        checkEventTrade(events.get(2), 1L, 81600L, 100L);

        //        // check orders are removed from map
        //        assertNull(orderBook.getOrderById(1L));
        //        assertNull(orderBook.getOrderById(2L));
        //        assertNull(orderBook.getOrderById(3L));
        //    }

        //    @Test
        //public void shouldMatchIocOrderWithRejection()
        //    {

        //        // size=270
        //        OrderCommand cmd = OrderCommand.newOrder(IOC, 123, UID_2, MAX_PRICE, MAX_PRICE + 1, 270, OrderAction.BID);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
        //        // all OrderAction.ASKs matched
        //        L2MarketData expected = expectedState.removeAllOrderAction.ASKs().build();
        //        assertEquals(expected, snapshot);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (7));

        //        // 6 trades generated, first comes rejection with size=25 left unmatched
        //        checkEventRejection(events.get(0), 25L, 400000L, MAX_PRICE + 1);
        //    }

        //    // ---------------------- FOK BUDGET ORDERS ---------------------------

        //    @Test
        //public void shouldRejectFokOrderAction.BIDOrderOutOfBudget()
        //    {

        //        long size = 180L;
        //        long buyBudget = expectedState.aggregateBuyBudget(size) - 1;
        //        assertThat(buyBudget, Is.is (81599L * 75L + 81600L * 100L + 200954L * 5L - 1));

        //        OrderCommand cmd = OrderCommand.newOrder(FOK_BUDGET, 123L, UID_2, buyBudget, buyBudget, size, OrderAction.BID);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
        //        assertEquals(expectedState.build(), snapshot);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (1));

        //        // no trades generated, rejection with full size unmatched
        //        checkEventRejection(events.get(0), size, buyBudget, buyBudget);
        //    }

        //    @Test
        //public void shouldMatchFokOrderAction.BIDOrderExactBudget()
        //    {

        //        long size = 180L;
        //        long buyBudget = expectedState.aggregateBuyBudget(size);
        //        assertThat(buyBudget, Is.is (81599L * 75L + 81600L * 100L + 200954L * 5L));

        //        OrderCommand cmd = OrderCommand.newOrder(FOK_BUDGET, 123L, UID_2, buyBudget, buyBudget, size, OrderAction.BID);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
        //        assertEquals(expectedState.removeOrderAction.ASK(0).removeOrderAction.ASK(0).setOrderAction.ASKVolume(0, 5).build(), snapshot);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (4));
        //        checkEventTrade(events.get(0), 2L, 81599, 50L);
        //        checkEventTrade(events.get(1), 3L, 81599, 25L);
        //        checkEventTrade(events.get(2), 1L, 81600L, 100L);
        //        checkEventTrade(events.get(3), 10L, 200954L, 5L);
        //    }

        //    @Test
        //public void shouldMatchFokOrderAction.BIDOrderExtraBudget()
        //    {

        //        long size = 176L;
        //        long buyBudget = expectedState.aggregateBuyBudget(size) + 1;
        //        assertThat(buyBudget, Is.is (81599L * 75L + 81600L * 100L + 200954L + 1L));

        //        OrderCommand cmd = OrderCommand.newOrder(FOK_BUDGET, 123L, UID_2, buyBudget, buyBudget, size, OrderAction.BID);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
        //        assertEquals(expectedState.removeOrderAction.ASK(0).removeOrderAction.ASK(0).setOrderAction.ASKVolume(0, 9).build(), snapshot);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (4));
        //        checkEventTrade(events.get(0), 2L, 81599, 50L);
        //        checkEventTrade(events.get(1), 3L, 81599, 25L);
        //        checkEventTrade(events.get(2), 1L, 81600L, 100L);
        //        checkEventTrade(events.get(3), 10L, 200954L, 1L);
        //    }

        //    @Test
        //public void shouldRejectFokOrderAction.ASKOrderBelowExpectation()
        //    {

        //        long size = 60L;
        //        long sellExpectation = expectedState.aggregateSellExpectation(size) + 1;
        //        assertThat(sellExpectation, Is.is (81593L * 40L + 81590L * 20L + 1));

        //        OrderCommand cmd = OrderCommand.newOrder(FOK_BUDGET, 123L, UID_2, sellExpectation, sellExpectation, size, OrderAction.ASK);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
        //        assertEquals(expectedState.build(), snapshot);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (1));
        //        // no trades generated, rejection with full size unmatched
        //        checkEventRejection(events.get(0), size, sellExpectation, sellExpectation);
        //    }

        //    @Test
        //public void shouldMatchFokOrderAction.ASKOrderExactExpectation()
        //    {

        //        long size = 60L;
        //        long sellExpectation = expectedState.aggregateSellExpectation(size);
        //        assertThat(sellExpectation, Is.is (81593L * 40L + 81590L * 20L));

        //        OrderCommand cmd = OrderCommand.newOrder(FOK_BUDGET, 123L, UID_2, sellExpectation, sellExpectation, size, OrderAction.ASK);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
        //        assertEquals(expectedState.removeOrderAction.BID(0).setOrderAction.BIDVolume(0, 1).decrementOrderAction.BIDOrdersNum(0).build(), snapshot);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (2));
        //        checkEventTrade(events.get(0), 4L, 81593L, 40L);
        //        checkEventTrade(events.get(1), 5L, 81590L, 20L);
        //    }

        //    @Test
        //public void shouldMatchFokOrderAction.ASKOrderExtraBudget()
        //    {

        //        long size = 61L;
        //        long sellExpectation = expectedState.aggregateSellExpectation(size) - 1;
        //        assertThat(sellExpectation, Is.is (81593L * 40L + 81590L * 21L - 1));

        //        OrderCommand cmd = OrderCommand.newOrder(FOK_BUDGET, 123L, UID_2, sellExpectation, sellExpectation, size, OrderAction.ASK);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
        //        assertEquals(expectedState.removeOrderAction.BID(0).removeOrderAction.BID(0).build(), snapshot);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (3));
        //        checkEventTrade(events.get(0), 4L, 81593L, 40L);
        //        checkEventTrade(events.get(1), 5L, 81590L, 20L);
        //        checkEventTrade(events.get(2), 6L, 81590L, 1L);
        //    }


        //    // MARKETABLE OrderType.GTC ORDERS

        //    @Test
        //public void shouldFullyMatchMarketableOrderType.GTCOrder()
        //    {

        //        // size=1
        //        OrderCommand cmd = OrderCommand.newOrder(OrderType.GTC, 123, UID_2, 81599, MAX_PRICE, 1, OrderAction.BID);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
        //        // best OrderAction.ASK partially matched
        //        L2MarketData expected = expectedState.setOrderAction.ASKVolume(0, 74).build();
        //        assertEquals(expected, snapshot);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (1));
        //        checkEventTrade(events.get(0), 2L, 81599, 1L);
        //    }


        //    @Test
        //public void shouldPartiallyMatchMarketableOrderType.GTCOrderAndPlace()
        //    {

        //        // size=77
        //        OrderCommand cmd = OrderCommand.newOrder(OrderType.GTC, 123, UID_2, 81599, MAX_PRICE, 77, OrderAction.BID);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
        //        // best OrderAction.ASKs fully matched, limit OrderAction.BID order placed
        //        L2MarketData expected = expectedState.removeOrderAction.ASK(0).insertOrderAction.BID(0, 81599, 2).build();
        //        assertEquals(expected, snapshot);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (2));

        //        checkEventTrade(events.get(0), 2L, 81599, 50L);
        //        checkEventTrade(events.get(1), 3L, 81599, 25L);
        //    }

        //    @Test
        //public void shouldFullyMatchMarketableOrderType.GTCOrder2Prices()
        //    {

        //        // size=77
        //        OrderCommand cmd = OrderCommand.newOrder(OrderType.GTC, 123, UID_2, 81600, MAX_PRICE, 77, OrderAction.BID);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
        //        // best OrderAction.ASKs fully matched, limit OrderAction.BID order placed
        //        L2MarketData expected = expectedState.removeOrderAction.ASK(0).setOrderAction.ASKVolume(0, 98).build();
        //        assertEquals(expected, snapshot);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (3));

        //        checkEventTrade(events.get(0), 2L, 81599, 50L);
        //        checkEventTrade(events.get(1), 3L, 81599, 25L);
        //        checkEventTrade(events.get(2), 1L, 81600, 2L);
        //    }


        //    @Test
        //public void shouldFullyMatchMarketableOrderType.GTCOrderWithAllLiquidity()
        //    {

        //        // size=1000
        //        OrderCommand cmd = OrderCommand.newOrder(OrderType.GTC, 123, UID_2, 220000, MAX_PRICE, 1000, OrderAction.BID);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);
        //        // best OrderAction.ASKs fully matched, limit OrderAction.BID order placed
        //        L2MarketData expected = expectedState.removeAllOrderAction.ASKs().insertOrderAction.BID(0, 220000, 755).build();
        //        assertEquals(expected, snapshot);

        //        // trades only, rejection not generated for limit order
        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (6));

        //        checkEventTrade(events.get(0), 2L, 81599, 50L);
        //        checkEventTrade(events.get(1), 3L, 81599, 25L);
        //        checkEventTrade(events.get(2), 1L, 81600, 100L);
        //        checkEventTrade(events.get(3), 10L, 200954, 10L);
        //        checkEventTrade(events.get(4), 8L, 201000, 28L);
        //        checkEventTrade(events.get(5), 9L, 201000, 32L);
        //    }


        //    // Move OrderType.GTC order to marketable price
        //    // TODO add into far area
        //    @Test
        //public void shouldMoveOrderFullyMatchAsMarketable()
        //    {

        //        // add new order and check it is there
        //        OrderCommand cmd = OrderCommand.newOrder(OrderType.GTC, 83, UID_2, 81200, MAX_PRICE, 20, OrderAction.BID);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (0));

        //        L2MarketData expected = expectedState.setOrderAction.BIDVolume(2, 40).incrementOrderAction.BIDOrdersNum(2).build();
        //        assertEquals(expected, orderBook.getL2MarketDataSnapshot(10));

        //        // move to marketable price area
        //        cmd = OrderCommand.update(83, UID_2, 81602);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        // moved
        //        expected = expectedState.setOrderAction.BIDVolume(2, 20).decrementOrderAction.BIDOrdersNum(2).setOrderAction.ASKVolume(0, 55).build();
        //        assertEquals(expected, orderBook.getL2MarketDataSnapshot(10));

        //        events = cmd.extractEvents();
        //        assertThat(events.size(), is (1));
        //        checkEventTrade(events.get(0), 2L, 81599, 20L);
        //    }


        //    @Test
        //public void shouldMoveOrderFullyMatchAsMarketable2Prices()
        //    {

        //        OrderCommand cmd = OrderCommand.newOrder(OrderType.GTC, 83, UID_2, 81594, MAX_PRICE, 100, OrderAction.BID);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (0));

        //        // move to marketable zone
        //        cmd = OrderCommand.update(83, UID_2, 81600);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);

        //        // moved
        //        L2MarketData expected = expectedState.removeOrderAction.ASK(0).setOrderAction.ASKVolume(0, 75).build();
        //        assertEquals(expected, snapshot);

        //        events = cmd.extractEvents();
        //        assertThat(events.size(), is (3));
        //        checkEventTrade(events.get(0), 2L, 81599, 50L);
        //        checkEventTrade(events.get(1), 3L, 81599, 25L);
        //        checkEventTrade(events.get(2), 1L, 81600, 25L);

        //    }

        //    @Test
        //public void shouldMoveOrderMatchesAllLiquidity()
        //    {

        //        OrderCommand cmd = OrderCommand.newOrder(OrderType.GTC, 83, UID_2, 81594, MAX_PRICE, 246, OrderAction.BID);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        // move to marketable zone
        //        cmd = OrderCommand.update(83, UID_2, 201000);
        //        processAndValidate(cmd, CommandResultCode.SUCCESS);

        //        L2MarketData snapshot = orderBook.getL2MarketDataSnapshot(10);

        //        // moved
        //        L2MarketData expected = expectedState.removeAllOrderAction.ASKs().insertOrderAction.BID(0, 201000, 1).build();
        //        assertEquals(expected, snapshot);

        //        List<MatcherTradeEvent> events = cmd.extractEvents();
        //        assertThat(events.size(), is (6));
        //        checkEventTrade(events.get(0), 2L, 81599, 50L);
        //        checkEventTrade(events.get(1), 3L, 81599, 25L);
        //        checkEventTrade(events.get(2), 1L, 81600, 100L);
        //        checkEventTrade(events.get(3), 10L, 200954, 10L);
        //        checkEventTrade(events.get(4), 8L, 201000, 28L);
        //        checkEventTrade(events.get(5), 9L, 201000, 32L);
        //    }


        //    @Test
        //public void multipleCommandsKeepInternalStateTest()
        //    {

        //        int tranNum = 25000;

        //        final IOrderBook localOrderBook = createNewOrderBook();
        //        localOrderBook.validateInternalState();

        //        TestOrdersGenerator.GenResult genResult = TestOrdersGenerator.generateCommands(
        //                tranNum,
        //                200,
        //                6,
        //                TestOrdersGenerator.UID_PLAIN_MAPPER,
        //                0,
        //                false,
        //                false,
        //                TestOrdersGenerator.createAsyncProgressLogger(tranNum),
        //                348290254);

        //        genResult.getCommands().forEach(cmd-> {
        //            cmd.orderId += 100; // TODO set start id
        //                                //log.debug("{}",  cmd);
        //            CommandResultCode commandResultCode = IOrderBook.processCommand(localOrderBook, cmd);
        //            assertThat(commandResultCode, is (CommandResultCode.SUCCESS));
        //            localOrderBook.validateInternalState();
        //        });

        //    }

        //    // ------------------------------- UTILITY METHODS --------------------------

        public void processAndValidate(OrderCommand cmd, CommandResultCode expectedCmdState)
        {
            CommandResultCode resultCode = IOrderBook.processCommand(orderBook, cmd);
            Assert.AreEqual(resultCode, expectedCmdState);
            orderBook.validateInternalState();
        }

        //    public void checkEventTrade(MatcherTradeEvent event, long matchedId, long price, long size) {
        //    assertThat(event.eventType, is(MatcherEventType.TRADE));
        //    assertThat(event.matchedOrderId, is(matchedId));
        //    assertThat(event.price, is(price));
        //    assertThat(event.size, is(size));
        //    // TODO add more checks for MatcherTradeEvent
        //}

        //public void checkEventRejection(MatcherTradeEvent event, long size, long price, Long OrderAction.BIDderHoldPrice)
        //{
        //    assertThat(event.eventType, is (MatcherEventType.REJECT));
        //    assertThat(event.size, is (size));
        //    assertThat(event.price, is (price));
        //    assertTrue(event.activeOrderCompleted);
        //    if (OrderAction.BIDderHoldPrice != null)
        //    {
        //        assertThat(event.OrderAction.BIDderHoldPrice, is (OrderAction.BIDderHoldPrice));
        //    }
        //}

        //public void checkEventReduce(MatcherTradeEvent event, long reduceSize, long price, boolean completed, Long OrderAction.BIDderHoldPrice)
        //{
        //    assertThat(event.eventType, is (MatcherEventType.REDUCE));
        //    assertThat(event.size, is (reduceSize));
        //    assertThat(event.price, is (price));
        //    assertThat(event.activeOrderCompleted, is (completed));
        //    assertNull(event.nextEvent);
        //    if (OrderAction.BIDderHoldPrice != null)
        //    {
        //        assertThat(event.OrderAction.BIDderHoldPrice, is (OrderAction.BIDderHoldPrice));
        //    }
        //}

    }
}

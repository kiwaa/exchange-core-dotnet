using Exchange.Core.Common;
using Exchange.Core.Common.Api;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Common.Config;
using Exchange.Core.Tests.Utils;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Tests.Integration
{
    public abstract class ITExchangeCoreIntegrationRejection
    {
        private SimpleEventsProcessor processor;

        private Mock<IEventsHandler> handler;
        private RejectEvent _rejectEvent;
        private TradeEvent _tradeEvent;

        [SetUp]
        public void SetUp()
        {
            handler = new Mock<IEventsHandler>();
            _rejectEvent = null;
            handler.Setup(h => h.rejectEvent(It.IsAny<RejectEvent>()))
                    .Callback<RejectEvent>(r => _rejectEvent = r);
            _tradeEvent = null;
            handler.Setup(h => h.tradeEvent(It.IsAny<TradeEvent>()))
                    .Callback<TradeEvent>(r => _tradeEvent = r);

            processor = new SimpleEventsProcessor(handler.Object);
        }


        // -------------------------- buy no rejection tests -----------------------------

        [Test, Timeout(5000)]
        public void testMultiBuyNoRejectionMarginGtc()
        {
            testMultiBuy(TestConstants.SYMBOLSPECFEE_USD_JPY, OrderType.GTC, RejectionCause.NO_REJECTION);
        }

        [Test, Timeout(5000)]
        public void testMultiBuyNoRejectionExchangeGtc()
        {
            testMultiBuy(TestConstants.SYMBOLSPECFEE_XBT_LTC, OrderType.GTC, RejectionCause.NO_REJECTION);
        }

        [Test, Timeout(5000)]
        public void testMultiBuyNoRejectionExchangeIoc()
        {
            testMultiBuy(TestConstants.SYMBOLSPECFEE_XBT_LTC, OrderType.IOC, RejectionCause.NO_REJECTION);
        }

        [Test, Timeout(5000)]
        public void testMultiBuyNoRejectionMarginIoc()
        {
            testMultiBuy(TestConstants.SYMBOLSPECFEE_USD_JPY, OrderType.IOC, RejectionCause.NO_REJECTION);
        }

        [Test, Timeout(5000)]
        public void testMultiBuyNoRejectionExchangeFokB()
        {
            testMultiBuy(TestConstants.SYMBOLSPECFEE_XBT_LTC, OrderType.FOK_BUDGET, RejectionCause.NO_REJECTION);
        }

        [Test, Timeout(5000)]
        public void testMultiBuyNoRejectionMarginFokB()
        {
            testMultiBuy(TestConstants.SYMBOLSPECFEE_USD_JPY, OrderType.FOK_BUDGET, RejectionCause.NO_REJECTION);
        }

        // -------------------------- buy with rejection tests -----------------------------

        [Test, Timeout(5000)]
        public void testMultiBuyWithRejectionMarginGtc()
        {
            testMultiBuy(TestConstants.SYMBOLSPECFEE_USD_JPY, OrderType.GTC, RejectionCause.REJECTION_BY_SIZE);
        }

        [Test, Timeout(5000)]
        public void testMultiBuyWithRejectionExchangeGtc()
        {
            testMultiBuy(TestConstants.SYMBOLSPECFEE_XBT_LTC, OrderType.GTC, RejectionCause.REJECTION_BY_SIZE);
        }

        [Test, Timeout(5000)]
        public void testMultiBuyWithRejectionExchangeIoc()
        {
            testMultiBuy(TestConstants.SYMBOLSPECFEE_XBT_LTC, OrderType.IOC, RejectionCause.REJECTION_BY_SIZE);
        }

        [Test, Timeout(5000)]
        public void testMultiBuyWithRejectionMarginIoc()
        {
            testMultiBuy(TestConstants.SYMBOLSPECFEE_USD_JPY, OrderType.IOC, RejectionCause.REJECTION_BY_SIZE);
        }

        [Test, Timeout(5000)]
        public void testMultiBuyWithSizeRejectionExchangeFokB()
        {
            testMultiBuy(TestConstants.SYMBOLSPECFEE_XBT_LTC, OrderType.FOK_BUDGET, RejectionCause.REJECTION_BY_SIZE);
        }

        [Test, Timeout(5000)]
        public void testMultiBuyWithSizeRejectionMarginFokB()
        {
            testMultiBuy(TestConstants.SYMBOLSPECFEE_USD_JPY, OrderType.FOK_BUDGET, RejectionCause.REJECTION_BY_SIZE);
        }

        [Test, Timeout(5000)]
        public void testMultiBuyWithBudgetRejectionExchangeFokB()
        {
            testMultiBuy(TestConstants.SYMBOLSPECFEE_XBT_LTC, OrderType.FOK_BUDGET, RejectionCause.REJECTION_BY_BUDGET);
        }

        [Test, Timeout(5000)]
        public void testMultiBuyWithBudgetRejectionMarginFokB()
        {
            testMultiBuy(TestConstants.SYMBOLSPECFEE_USD_JPY, OrderType.FOK_BUDGET, RejectionCause.REJECTION_BY_BUDGET);
        }

        // -------------------------- sell no rejection tests -----------------------------

        [Test, Timeout(5000)]
        public void testMultiSellNoRejectionMarginGtc()
        {
            testMultiSell(TestConstants.SYMBOLSPECFEE_USD_JPY, OrderType.GTC, RejectionCause.NO_REJECTION);
        }

        [Test, Timeout(5000)]
        public void testMultiSellNoRejectionExchangeGtc()
        {
            testMultiSell(TestConstants.SYMBOLSPECFEE_XBT_LTC, OrderType.GTC, RejectionCause.NO_REJECTION);
        }

        [Test, Timeout(5000)]
        public void testMultiSellNoRejectionMarginIoc()
        {
            testMultiSell(TestConstants.SYMBOLSPECFEE_USD_JPY, OrderType.IOC, RejectionCause.NO_REJECTION);
        }

        [Test, Timeout(5000)]
        public void testMultiSellNoRejectionExchangeIoc()
        {
            testMultiSell(TestConstants.SYMBOLSPECFEE_XBT_LTC, OrderType.IOC, RejectionCause.NO_REJECTION);
        }

        [Test, Timeout(5000)]
        public void testMultiSellNoRejectionMarginFokB()
        {
            testMultiSell(TestConstants.SYMBOLSPECFEE_USD_JPY, OrderType.FOK_BUDGET, RejectionCause.NO_REJECTION);
        }

        [Test, Timeout(5000)]
        public void testMultiSellNoRejectionExchangeFokB()
        {
            testMultiSell(TestConstants.SYMBOLSPECFEE_XBT_LTC, OrderType.FOK_BUDGET, RejectionCause.NO_REJECTION);
        }

        // -------------------------- sell with rejection tests -----------------------------

        [Test, Timeout(5000)]
        public void testMultiSellWithRejectionMarginGtc()
        {
            testMultiSell(TestConstants.SYMBOLSPECFEE_USD_JPY, OrderType.GTC, RejectionCause.REJECTION_BY_SIZE);
        }

        [Test, Timeout(5000)]
        public void testMultiSellWithRejectionExchangeGtc()
        {
            testMultiSell(TestConstants.SYMBOLSPECFEE_XBT_LTC, OrderType.GTC, RejectionCause.REJECTION_BY_SIZE);
        }

        [Test, Timeout(5000)]
        public void testMultiSellWithRejectionMarginIoc()
        {
            testMultiSell(TestConstants.SYMBOLSPECFEE_USD_JPY, OrderType.IOC, RejectionCause.REJECTION_BY_SIZE);
        }

        [Test, Timeout(5000)]
        public void testMultiSellWithRejectionExchangeIoc()
        {
            testMultiSell(TestConstants.SYMBOLSPECFEE_XBT_LTC, OrderType.IOC, RejectionCause.REJECTION_BY_SIZE);
        }

        [Test, Timeout(5000)]
        public void testMultiSellWithSizeRejectionMarginFokB()
        {
            testMultiSell(TestConstants.SYMBOLSPECFEE_USD_JPY, OrderType.FOK_BUDGET, RejectionCause.REJECTION_BY_SIZE);
        }

        [Test, Timeout(5000)]
        public void testMultiSellWithSizeRejectionExchangeFokB()
        {
            testMultiSell(TestConstants.SYMBOLSPECFEE_XBT_LTC, OrderType.FOK_BUDGET, RejectionCause.REJECTION_BY_SIZE);
        }

        [Test, Timeout(5000)]
        public void testMultiSellWithExpectationRejectionMarginFokB()
        {
            testMultiSell(TestConstants.SYMBOLSPECFEE_USD_JPY, OrderType.FOK_BUDGET, RejectionCause.REJECTION_BY_BUDGET);
        }

        [Test, Timeout(5000)]
        public void testMultiSellWithExpectationRejectionExchangeFokB()
        {
            testMultiSell(TestConstants.SYMBOLSPECFEE_XBT_LTC, OrderType.FOK_BUDGET, RejectionCause.REJECTION_BY_BUDGET);
        }

        // configuration provided by child class
        public abstract PerformanceConfiguration getPerformanceConfiguration();

        // ------------------------------------------------------------------------------

        private ApiPlaceOrder.ApiPlaceOrderBuilder builderPlace(int symbolId, long uid, OrderAction action, OrderType type)
        {
            return ApiPlaceOrder.Builder().uid(uid).action(action).orderType(type).symbol(symbolId);
        }

        // TODO count/verify number of commands and events
        private void testMultiBuy(CoreSymbolSpecification symbolSpec, OrderType orderType, RejectionCause rejectionCause)
        {

            int symbolId = symbolSpec.SymbolId;

            long size = 40L + (rejectionCause == RejectionCause.REJECTION_BY_SIZE ? 1 : 0);

            using (ExchangeTestContainer container = ExchangeTestContainer.create(getPerformanceConfiguration()))
            {
                container.initFeeSymbols();
                container.initFeeUsers();

                container.consumer = processor.accept;

                container.submitCommandSync(builderPlace(symbolId, TestConstants.UID_1, OrderAction.ASK, OrderType.GTC).orderId(101L).price(160000L).size(7L).build(), CommandResultCode.SUCCESS);
                container.submitCommandSync(builderPlace(symbolId, TestConstants.UID_2, OrderAction.ASK, OrderType.GTC).orderId(202L).price(159900L).size(10L).build(), CommandResultCode.SUCCESS);
                container.submitCommandSync(builderPlace(symbolId, TestConstants.UID_3, OrderAction.ASK, OrderType.GTC).orderId(303L).price(160000L).size(3L).build(), CommandResultCode.SUCCESS);
                container.submitCommandSync(builderPlace(symbolId, TestConstants.UID_3, OrderAction.ASK, OrderType.GTC).orderId(304L).price(160500L).size(20L).build(), CommandResultCode.SUCCESS);


                long price = 160500L;
                if (orderType == OrderType.FOK_BUDGET)
                {
                    price = 160000L * 7L + 159900L * 10L + 160000L * 3L + 160500L * 20L + (rejectionCause == RejectionCause.REJECTION_BY_BUDGET ? -1 : 0);
                }

                container.submitCommandSync(builderPlace(symbolId, TestConstants.UID_4, OrderAction.BID, orderType).orderId(405L).price(price).reservePrice(price).size(size).build(), CommandResultCode.SUCCESS);

                Assert.AreEqual(container.totalBalanceReport().isGlobalBalancesAllZero(), true);
            }

            //verify(handler, times(5)).commandResult(commandResultCaptor.capture());
            handler.Verify(x => x.commandResult(It.IsAny<ApiCommandResult>()), Times.Exactly(5));
            //verify(handler, never()).reduceEvent(any());
            handler.Verify(x => x.reduceEvent(It.IsAny<ReduceEvent>()), Times.Never);

            if (orderType == OrderType.FOK_BUDGET && rejectionCause != RejectionCause.NO_REJECTION)
            {
                // no trades for FoK
                //verify(handler, never()).tradeEvent(any());
                handler.Verify(x => x.tradeEvent(It.IsAny<TradeEvent>()), Times.Never);

            }
            else
            {
                //verify(handler, times(1)).tradeEvent(tradeEventCaptor.capture());
                handler.Verify(x => x.tradeEvent(It.IsAny<TradeEvent>()), Times.Exactly(1));

                // validating first event
                TradeEvent tradeEvent = _tradeEvent;
                Assert.AreEqual(tradeEvent.Symbol, symbolId);
                Assert.AreEqual(tradeEvent.TotalVolume, 40L);
                Assert.AreEqual(tradeEvent.TakerOrderId, 405L);
                Assert.AreEqual(tradeEvent.TakerUid, TestConstants.UID_4);
                Assert.AreEqual(tradeEvent.TakerAction, OrderAction.BID);
                Assert.AreEqual(tradeEvent.TakeOrderCompleted, rejectionCause == RejectionCause.NO_REJECTION); // completed only if no rejection was happened

                List<Trade> trades = tradeEvent.Trades;
                Assert.AreEqual(trades.Count, 4);

                Assert.AreEqual(trades[0].MakerOrderId, 202L);
                Assert.AreEqual(trades[0].MakerUid, TestConstants.UID_2);
                Assert.AreEqual(trades[0].MakerOrderCompleted, true);
                Assert.AreEqual(trades[0].Price, 159900L);
                Assert.AreEqual(trades[0].Volume, 10L);

                Assert.AreEqual(trades[1].MakerOrderId, 101L);
                Assert.AreEqual(trades[1].MakerUid, TestConstants.UID_1);
                Assert.AreEqual(trades[1].MakerOrderCompleted, true);
                Assert.AreEqual(trades[1].Price, 160000L);
                Assert.AreEqual(trades[1].Volume, 7L);

                Assert.AreEqual(trades[2].MakerOrderId, 303L);
                Assert.AreEqual(trades[2].MakerUid, TestConstants.UID_3);
                Assert.AreEqual(trades[2].MakerOrderCompleted, true);
                Assert.AreEqual(trades[2].Price, 160000L);
                Assert.AreEqual(trades[2].Volume, 3L);

                Assert.AreEqual(trades[3].MakerOrderId, 304L);
                Assert.AreEqual(trades[3].MakerUid, TestConstants.UID_3);
                Assert.AreEqual(trades[3].MakerOrderCompleted, true);
                Assert.AreEqual(trades[3].Price, 160500L);
                Assert.AreEqual(trades[3].Volume, 20L);
            }

            if (rejectionCause != RejectionCause.NO_REJECTION && orderType != OrderType.GTC)
            { // rejection can not happen for GTC orders
                //verify(handler, times(1)).rejectEvent(rejectEventCaptor.capture());
                handler.Verify(x => x.rejectEvent(It.IsAny<RejectEvent>()), Times.Exactly(1));
                RejectEvent rejectEvent = _rejectEvent;
                Assert.AreEqual(rejectEvent.Symbol, symbolId);
                Assert.AreEqual(rejectEvent.RejectedVolume, (orderType == OrderType.FOK_BUDGET) ? size : 1L);
                Assert.AreEqual(rejectEvent.OrderId, 405L);
                Assert.AreEqual(rejectEvent.Uid, TestConstants.UID_4);
            }
            else
            {
                //verify(handler, never()).rejectEvent(any());
                handler.Verify(x => x.rejectEvent(It.IsAny<RejectEvent>()), Times.Never);
            }

        }

        private void testMultiSell(CoreSymbolSpecification symbolSpec, OrderType orderType, RejectionCause rejectionCause)
        {

            int symbolId = symbolSpec.SymbolId;

            long size = 22L + (rejectionCause == RejectionCause.REJECTION_BY_SIZE ? 1 : 0);

            using (ExchangeTestContainer container = ExchangeTestContainer.create(getPerformanceConfiguration()))
            {
                container.initFeeSymbols();
                container.initFeeUsers();

                container.consumer = processor.accept;

                long price = 159_900L;
                if (orderType == OrderType.FOK_BUDGET)
                {
                    price = 160_500L + 160_000L * 20L + 159_900L + (rejectionCause == RejectionCause.REJECTION_BY_BUDGET ? 1 : 0);
                }

                container.submitCommandSync(builderPlace(symbolId, TestConstants.UID_1, OrderAction.BID, OrderType.GTC).orderId(101L).price(160_000L).reservePrice(166_000L).size(12L).build(), CommandResultCode.SUCCESS);
                container.submitCommandSync(builderPlace(symbolId, TestConstants.UID_2, OrderAction.BID, OrderType.GTC).orderId(202L).price(159_900L).reservePrice(166_000L).size(1L).build(), CommandResultCode.SUCCESS);
                container.submitCommandSync(builderPlace(symbolId, TestConstants.UID_3, OrderAction.BID, OrderType.GTC).orderId(303L).price(160_000L).reservePrice(166_000L).size(8L).build(), CommandResultCode.SUCCESS);
                container.submitCommandSync(builderPlace(symbolId, TestConstants.UID_3, OrderAction.BID, OrderType.GTC).orderId(304L).price(160_500L).reservePrice(166_000L).size(1L).build(), CommandResultCode.SUCCESS);

                container.submitCommandSync(builderPlace(symbolId, TestConstants.UID_4, OrderAction.ASK, orderType).orderId(405L).price(price).size(size).build(), CommandResultCode.SUCCESS);

                Assert.AreEqual(container.totalBalanceReport().isGlobalBalancesAllZero(), true);
            }

            //verify(handler, times(5)).commandResult(commandResultCaptor.capture());
            handler.Verify(x => x.commandResult(It.IsAny<ApiCommandResult>()), Times.Exactly(5));
            //verify(handler, never()).reduceEvent(any());
            handler.Verify(x => x.reduceEvent(It.IsAny<ReduceEvent>()), Times.Never);

            if (orderType == OrderType.FOK_BUDGET && rejectionCause != RejectionCause.NO_REJECTION)
            {
                // no trades for FoK
                //verify(handler, never()).tradeEvent(any());
                handler.Verify(x => x.tradeEvent(It.IsAny<TradeEvent>()), Times.Never);
            }
            else
            {
                //verify(handler, times(1)).tradeEvent(tradeEventCaptor.capture());
                handler.Verify(x => x.tradeEvent(It.IsAny<TradeEvent>()), Times.Exactly(1));

                // validating first event
                TradeEvent tradeEvent = _tradeEvent;
                Assert.AreEqual(tradeEvent.Symbol, symbolId);
                Assert.AreEqual(tradeEvent.TotalVolume, 22L);
                Assert.AreEqual(tradeEvent.TakerOrderId, 405L);
                Assert.AreEqual(tradeEvent.TakerUid, TestConstants.UID_4);
                Assert.AreEqual(tradeEvent.TakerAction, OrderAction.ASK);
                Assert.AreEqual(tradeEvent.TakeOrderCompleted, rejectionCause == RejectionCause.NO_REJECTION); // completed only if no rejection was happened

                List<Trade> trades = tradeEvent.Trades;
                Assert.AreEqual(trades.Count, 4);

                Assert.AreEqual(trades[0].MakerOrderId, 304L);
                Assert.AreEqual(trades[0].MakerUid, TestConstants.UID_3);
                Assert.AreEqual(trades[0].MakerOrderCompleted, true);
                Assert.AreEqual(trades[0].Price, 160500L);
                Assert.AreEqual(trades[0].Volume, 1L);

                Assert.AreEqual(trades[1].MakerOrderId, 101L);
                Assert.AreEqual(trades[1].MakerUid, TestConstants.UID_1);
                Assert.AreEqual(trades[1].MakerOrderCompleted, true);
                Assert.AreEqual(trades[1].Price, 160000L);
                Assert.AreEqual(trades[1].Volume, 12L);

                Assert.AreEqual(trades[2].MakerOrderId, 303L);
                Assert.AreEqual(trades[2].MakerUid, TestConstants.UID_3);
                Assert.AreEqual(trades[2].MakerOrderCompleted, true);
                Assert.AreEqual(trades[2].Price, 160000L);
                Assert.AreEqual(trades[2].Volume, 8L);

                Assert.AreEqual(trades[3].MakerOrderId, 202L);
                Assert.AreEqual(trades[3].MakerUid, TestConstants.UID_2);
                Assert.AreEqual(trades[3].MakerOrderCompleted, true);
                Assert.AreEqual(trades[3].Price, 159900L);
                Assert.AreEqual(trades[3].Volume, 1L);
            }

            if (rejectionCause != RejectionCause.NO_REJECTION && orderType != OrderType.GTC)
            { // rejection can not happen for GTC orders
                //verify(handler, times(1)).rejectEvent(rejectEventCaptor.capture()); 
                handler.Verify(x => x.rejectEvent(It.IsAny<RejectEvent>()), Times.Exactly(1));
                RejectEvent rejectEvent = _rejectEvent;
                Assert.AreEqual(rejectEvent.Symbol, symbolId);
                Assert.AreEqual(rejectEvent.RejectedVolume, (orderType == OrderType.FOK_BUDGET) ? size : 1L);
                Assert.AreEqual(rejectEvent.OrderId, 405L);
                Assert.AreEqual(rejectEvent.Uid, TestConstants.UID_4);
            }
            else
            {
                //verify(handler, never()).rejectEvent(any());
                handler.Verify(x => x.rejectEvent(It.IsAny<RejectEvent>()), Times.Never);
            }

        }

        enum RejectionCause
        {
            NO_REJECTION,
            REJECTION_BY_SIZE,
            REJECTION_BY_BUDGET
        }

    }
}

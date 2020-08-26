using Exchange.Core.Common;
using Exchange.Core.Common.Cmd;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Exchange.Core.Common.Api;

namespace Exchange.Core.Tests.Core
{
    [TestFixture]
    public class SimpleEventsProcessorTest
    {
        private Mock<IEventsHandler> _handler;
        private SimpleEventsProcessor _processor;

        //    @Captor
        //private ArgumentCaptor<IEventsHandler.ApiCommandResult> commandResultCaptor;

        //    @Captor
        //private ArgumentCaptor<IEventsHandler.ReduceEvent> reduceEventCaptor;

        //    @Captor
        //private ArgumentCaptor<IEventsHandler.TradeEvent> tradeEventCaptor;

        //    @Captor
        //private ArgumentCaptor<IEventsHandler.RejectEvent> rejectEventCaptor;

        [SetUp]
        public void SetUp()
        {
            _handler = new Mock<IEventsHandler>();
            _processor = new SimpleEventsProcessor(_handler.Object);
        }

        [Test]
        public void shouldHandleSimpleCommand()
        {
            ApiCommandResult resultCapture = null;
            _handler.Setup(h => h.commandResult(It.IsAny<ApiCommandResult>()))
                    .Callback<ApiCommandResult>(r => resultCapture = r);

            OrderCommand cmd = sampleCancelCommand();

            _processor.accept(cmd, 192837L);

            _handler.Verify(x => x.commandResult(It.IsAny<ApiCommandResult>()), Times.Once);
            _handler.Verify(x => x.tradeEvent(It.IsAny<TradeEvent>()), Times.Never);
            _handler.Verify(x => x.rejectEvent(It.IsAny<RejectEvent>()), Times.Never);
            _handler.Verify(x => x.reduceEvent(It.IsAny<ReduceEvent>()), Times.Never);

            var expected = ApiCancelOrder.Builder()
                                .orderId(123L)
                                .symbol(3)
                                .uid(29851L)
                                .build();
            Assert.AreEqual(resultCapture.Command, expected);
        }

        [Test]
        public void shouldHandleWithReduceCommand()
        {
            ApiCommandResult resultCapture = null;
            ReduceEvent reduceEvent = null;
            _handler.Setup(h => h.commandResult(It.IsAny<ApiCommandResult>()))
                    .Callback<ApiCommandResult>(r => resultCapture = r);
            _handler.Setup(h => h.reduceEvent(It.IsAny<ReduceEvent>()))
                    .Callback<ReduceEvent>(r => reduceEvent = r);

            OrderCommand cmd = sampleReduceCommand();

            cmd.MatcherEvent = MatcherTradeEvent.Builder()
                    .eventType(MatcherEventType.REDUCE)
                    .activeOrderCompleted(true)
                    .price(20100L)
                    .size(8272L)
                    .nextEvent(null)
                    .build();

            _processor.accept(cmd, 192837L);

            _handler.Verify(x => x.commandResult(It.IsAny<ApiCommandResult>()), Times.Once);
            _handler.Verify(x => x.tradeEvent(It.IsAny<TradeEvent>()), Times.Never);
            _handler.Verify(x => x.rejectEvent(It.IsAny<RejectEvent>()), Times.Never);
            _handler.Verify(x => x.reduceEvent(It.IsAny<ReduceEvent>()), Times.Once);

            var expected = ApiReduceOrder.Builder()
                            .orderId(123L)
                            .reduceSize(3200L)
                            .symbol(3)
                            .uid(29851L)
                            .build();

            Assert.AreEqual(resultCapture.Command, expected);

            Assert.AreEqual(reduceEvent.OrderId, 123L);
            Assert.AreEqual(reduceEvent.Price, 20100L);
            Assert.AreEqual(reduceEvent.ReducedVolume, 8272L);
            Assert.AreEqual(reduceEvent.OrderCompleted, true);
        }

        [Test]
        public void shouldHandleWithSingleTrade()
        {
            ApiCommandResult resultCapture = null;
            TradeEvent tradeEvent = null;
            _handler.Setup(h => h.commandResult(It.IsAny<ApiCommandResult>()))
                    .Callback<ApiCommandResult>(r => resultCapture = r);
            _handler.Setup(h => h.tradeEvent(It.IsAny<TradeEvent>()))
                    .Callback<TradeEvent>(r => tradeEvent = r);

            OrderCommand cmd = samplePlaceOrderCommand();

            cmd.MatcherEvent = MatcherTradeEvent.Builder()
                    .eventType(MatcherEventType.TRADE)
                    .activeOrderCompleted(false)
                    .matchedOrderId(276810L)
                    .matchedOrderUid(10332L)
                    .matchedOrderCompleted(true)
                    .price(20100L)
                    .size(8272L)
                    .nextEvent(null)
                    .build();


            _processor.accept(cmd, 192837L);


            _handler.Verify(x => x.commandResult(It.IsAny<ApiCommandResult>()), Times.Once);
            _handler.Verify(x => x.rejectEvent(It.IsAny<RejectEvent>()), Times.Never);
            _handler.Verify(x => x.reduceEvent(It.IsAny<ReduceEvent>()), Times.Never);
            _handler.Verify(x => x.tradeEvent(It.IsAny<TradeEvent>()), Times.Once);

            var expected = ApiPlaceOrder.Builder()
                            .orderId(123L)
                            .symbol(3)
                            .price(52200L)
                            .size(3200L)
                            .reservePrice(12800L)
                            .action(OrderAction.BID)
                            .orderType(OrderType.IOC)
                            .uid(29851)
                            .userCookie(44188)
                            .build();

            Assert.AreEqual(resultCapture.Command, expected);

            Assert.AreEqual(tradeEvent.Symbol, 3);
            Assert.AreEqual(tradeEvent.TotalVolume, 8272L);
            Assert.AreEqual(tradeEvent.TakerOrderId, 123L);
            Assert.AreEqual(tradeEvent.TakerUid, 29851L);
            Assert.AreEqual(tradeEvent.TakerAction, OrderAction.BID);
            Assert.AreEqual(tradeEvent.TakeOrderCompleted, false);

            List<Trade> trades = tradeEvent.Trades;
            Assert.AreEqual(trades.Count, 1);
            Trade trade = trades[0];

            Assert.AreEqual(trade.MakerOrderId, 276810L);
            Assert.AreEqual(trade.MakerUid, 10332L);
            Assert.AreEqual(trade.MakerOrderCompleted, true);
            Assert.AreEqual(trade.Price, 20100L);
            Assert.AreEqual(trade.Volume, 8272L);
        }


        [Test]
        public void shouldHandleWithTwoTrades()
        {
            ApiCommandResult resultCapture = null;
            TradeEvent tradeEvent = null;
            _handler.Setup(h => h.commandResult(It.IsAny<ApiCommandResult>()))
                    .Callback<ApiCommandResult>(r => resultCapture = r);
            _handler.Setup(h => h.tradeEvent(It.IsAny<TradeEvent>()))
                    .Callback<TradeEvent>(r => tradeEvent = r);


            OrderCommand cmd = samplePlaceOrderCommand();

            MatcherTradeEvent secondTrade = MatcherTradeEvent.Builder()
                    .eventType(MatcherEventType.TRADE)
                    .activeOrderCompleted(true)
                    .matchedOrderId(100293L)
                    .matchedOrderUid(1982L)
                    .matchedOrderCompleted(false)
                    .price(20110L)
                    .size(3121L)
                    .nextEvent(null)
                    .build();

            MatcherTradeEvent firstTrade = MatcherTradeEvent.Builder()
                    .eventType(MatcherEventType.TRADE)
                    .activeOrderCompleted(false)
                    .matchedOrderId(276810L)
                    .matchedOrderUid(10332L)
                    .matchedOrderCompleted(true)
                    .price(20100L)
                    .size(8272L)
                    .nextEvent(secondTrade)
                    .build();

            cmd.MatcherEvent = firstTrade;

            _processor.accept(cmd, 12981721239L);


            _handler.Verify(x => x.commandResult(It.IsAny<ApiCommandResult>()), Times.Once);
            _handler.Verify(x => x.rejectEvent(It.IsAny<RejectEvent>()), Times.Never);
            _handler.Verify(x => x.reduceEvent(It.IsAny<ReduceEvent>()), Times.Never);
            _handler.Verify(x => x.tradeEvent(It.IsAny<TradeEvent>()), Times.Once);

            var expected = ApiPlaceOrder.Builder()
                            .orderId(123L)
                            .symbol(3)
                            .price(52200L)
                            .size(3200L)
                            .reservePrice(12800L)
                            .action(OrderAction.BID)
                            .orderType(OrderType.IOC)
                            .uid(29851)
                            .userCookie(44188)
                            .build();

            Assert.AreEqual(resultCapture.Command, expected);

            // validating first event
            Assert.AreEqual(tradeEvent.Symbol, 3);
            Assert.AreEqual(tradeEvent.TotalVolume, 11393L);
            Assert.AreEqual(tradeEvent.TakerOrderId, 123L);
            Assert.AreEqual(tradeEvent.TakerUid, 29851L);
            Assert.AreEqual(tradeEvent.TakerAction, OrderAction.BID);
            Assert.AreEqual(tradeEvent.TakeOrderCompleted, true);

            List<Trade> trades = tradeEvent.Trades;
            Assert.AreEqual(trades.Count, 2);

            Trade trade = trades[0];
            Assert.AreEqual(trade.MakerOrderId, 276810L);
            Assert.AreEqual(trade.MakerUid, 10332L);
            Assert.AreEqual(trade.MakerOrderCompleted, true);
            Assert.AreEqual(trade.Price, 20100L);
            Assert.AreEqual(trade.Volume, 8272L);

            trade = trades[1];
            Assert.AreEqual(trade.MakerOrderId, 100293L);
            Assert.AreEqual(trade.MakerUid, 1982L);
            Assert.AreEqual(trade.MakerOrderCompleted, false);
            Assert.AreEqual(trade.Price, 20110L);
            Assert.AreEqual(trade.Volume, 3121L);
        }

        [Test]
        public void shouldHandleWithTwoTradesAndReject()
        {
            ApiCommandResult resultCapture = null;
            TradeEvent tradeEvent = null;
            RejectEvent rejectEvent = null;
            _handler.Setup(h => h.commandResult(It.IsAny<ApiCommandResult>()))
                    .Callback<ApiCommandResult>(r => resultCapture = r);
            _handler.Setup(h => h.tradeEvent(It.IsAny<TradeEvent>()))
                    .Callback<TradeEvent>(r => tradeEvent = r);
            _handler.Setup(h => h.rejectEvent(It.IsAny<RejectEvent>()))
                    .Callback<RejectEvent>(r => rejectEvent = r);

            OrderCommand cmd = samplePlaceOrderCommand();

            MatcherTradeEvent reject = MatcherTradeEvent.Builder()
                    .eventType(MatcherEventType.REJECT)
                    .activeOrderCompleted(true)
                    .size(8272L)
                    .nextEvent(null)
                    .build();
            MatcherTradeEvent secondTrade = MatcherTradeEvent.Builder()
                    .eventType(MatcherEventType.TRADE)
                    .activeOrderCompleted(true)
                    .matchedOrderId(100293L)
                    .matchedOrderUid(1982L)
                    .matchedOrderCompleted(false)
                    .price(20110L)
                    .size(3121L)
                    .nextEvent(reject)
                    .build();
            MatcherTradeEvent firstTrade = MatcherTradeEvent.Builder()
                    .eventType(MatcherEventType.TRADE)
                    .activeOrderCompleted(false)
                    .matchedOrderId(276810L)
                    .matchedOrderUid(10332L)
                    .matchedOrderCompleted(true)
                    .price(20100L)
                    .size(8272L)
                    .nextEvent(secondTrade)
                    .build();


            cmd.MatcherEvent = firstTrade;

            _processor.accept(cmd, 12981721239L);

            _handler.Verify(x => x.commandResult(It.IsAny<ApiCommandResult>()), Times.Once);
            _handler.Verify(x => x.rejectEvent(It.IsAny<RejectEvent>()), Times.Once);
            _handler.Verify(x => x.reduceEvent(It.IsAny<ReduceEvent>()), Times.Never);
            _handler.Verify(x => x.tradeEvent(It.IsAny<TradeEvent>()), Times.Once);

            var expected = ApiPlaceOrder.Builder()
                            .orderId(123L)
                            .symbol(3)
                            .price(52200L)
                            .size(3200L)
                            .reservePrice(12800L)
                            .action(OrderAction.BID)
                            .orderType(OrderType.IOC)
                            .uid(29851)
                            .userCookie(44188)
                            .build();
            Assert.AreEqual(resultCapture.Command, expected);

            // validating first event
            Assert.AreEqual(tradeEvent.Symbol, 3);
            Assert.AreEqual(tradeEvent.TotalVolume, 11393L);
            Assert.AreEqual(tradeEvent.TakerOrderId, 123L);
            Assert.AreEqual(tradeEvent.TakerUid, 29851L);
            Assert.AreEqual(tradeEvent.TakerAction, OrderAction.BID);
            Assert.AreEqual(tradeEvent.TakeOrderCompleted, true);

            List<Trade> trades = tradeEvent.Trades;
            Assert.AreEqual(trades.Count, 2);

            Trade trade = trades[0];
            Assert.AreEqual(trade.MakerOrderId, 276810L);
            Assert.AreEqual(trade.MakerUid, 10332L);
            Assert.AreEqual(trade.MakerOrderCompleted, true);
            Assert.AreEqual(trade.Price, 20100L);
            Assert.AreEqual(trade.Volume, 8272L);

            trade = trades[1];
            Assert.AreEqual(trade.MakerOrderId, 100293L);
            Assert.AreEqual(trade.MakerUid, 1982L);
            Assert.AreEqual(trade.MakerOrderCompleted, false);
            Assert.AreEqual(trade.Price, 20110L);
            Assert.AreEqual(trade.Volume, 3121L);
        }


        [Test]
        public void shouldHandlerWithSingleReject()
        {
            ApiCommandResult resultCapture = null;
            RejectEvent rejectEvent = null;
            _handler.Setup(h => h.commandResult(It.IsAny<ApiCommandResult>()))
                    .Callback<ApiCommandResult>(r => resultCapture = r);
            _handler.Setup(h => h.rejectEvent(It.IsAny<RejectEvent>()))
                    .Callback<RejectEvent>(r => rejectEvent = r);

            OrderCommand cmd = samplePlaceOrderCommand();

            cmd.MatcherEvent = MatcherTradeEvent.Builder()
                    .eventType(MatcherEventType.REJECT)
                    .activeOrderCompleted(true)
                    .size(8272L)
                    .price(52201L)
                    .nextEvent(null)
                    .build();

            _processor.accept(cmd, 192837L);


            _handler.Verify(x => x.commandResult(It.IsAny<ApiCommandResult>()), Times.Once);
            _handler.Verify(x => x.reduceEvent(It.IsAny<ReduceEvent>()), Times.Never);
            _handler.Verify(x => x.tradeEvent(It.IsAny<TradeEvent>()), Times.Never);
            _handler.Verify(x => x.rejectEvent(It.IsAny<RejectEvent>()), Times.Once);

            var expected = ApiPlaceOrder.Builder()
                            .orderId(123L)
                            .symbol(3)
                            .price(52200L)
                            .size(3200L)
                            .reservePrice(12800L)
                            .action(OrderAction.BID)
                            .orderType(OrderType.IOC)
                            .uid(29851L)
                            .userCookie(44188)
                            .build();
            Assert.AreEqual(resultCapture.Command, expected);

            Assert.AreEqual(rejectEvent.Symbol, 3);
            Assert.AreEqual(rejectEvent.OrderId, 123L);
            Assert.AreEqual(rejectEvent.RejectedVolume, 8272L);
            Assert.AreEqual(rejectEvent.Price, 52201L);
            Assert.AreEqual(rejectEvent.Uid, 29851L);
        }


        private OrderCommand sampleCancelCommand()
        {

            return OrderCommand.Builder()
                    .command(OrderCommandType.CANCEL_ORDER)
                    .orderId(123L)
                    .symbol(3)
                    .price(12800L)
                    .size(3L)
                    .reserveBidPrice(12800L)
                    .action(OrderAction.BID)
                    .orderType(OrderType.GTC)
                    .uid(29851L)
                    .timestamp(1578930983745201L)
                    .userCookie(44188)
                    .resultCode(CommandResultCode.MATCHING_INVALID_ORDER_BOOK_ID)
                    .matcherEvent(null)
                    .marketData(null)
                    .build();
        }


        private OrderCommand sampleReduceCommand()
        {

            return OrderCommand.Builder()
                    .command(OrderCommandType.REDUCE_ORDER)
                    .orderId(123L)
                    .symbol(3)
                    .price(52200L)
                    .size(3200L)
                    .reserveBidPrice(12800L)
                    .action(OrderAction.BID)
                    .orderType(OrderType.GTC)
                    .uid(29851L)
                    .timestamp(1578930983745201L)
                    .userCookie(44188)
                    .resultCode(CommandResultCode.SUCCESS)
                    .matcherEvent(null)
                    .marketData(null)
                    .build();
        }

        private OrderCommand samplePlaceOrderCommand()
        {

            return OrderCommand.Builder()
                    .command(OrderCommandType.PLACE_ORDER)
                    .orderId(123L)
                    .symbol(3)
                    .price(52200L)
                    .size(3200L)
                    .reserveBidPrice(12800L)
                    .action(OrderAction.BID)
                    .orderType(OrderType.IOC)
                    .uid(29851L)
                    .timestamp(1578930983745201L)
                    .userCookie(44188)
                    .resultCode(CommandResultCode.SUCCESS)
                    .matcherEvent(null)
                    .marketData(null)
                    .build();
        }

    }
}

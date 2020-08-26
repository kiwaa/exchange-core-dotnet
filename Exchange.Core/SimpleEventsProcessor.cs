using Exchange.Core.Common;
using Exchange.Core.Common.Api;
using Exchange.Core.Common.Cmd;
using System;
using System.Collections.Generic;

namespace Exchange.Core
{
    public class SimpleEventsProcessor //: ObjLongConsumer<OrderCommand>
    {
        private IEventsHandler _eventsHandler;

        public SimpleEventsProcessor(IEventsHandler eventsHandler)
        {
            _eventsHandler = eventsHandler;
        }

        public void accept(OrderCommand cmd, long seq)
        {
            try
            {
                sendCommandResult(cmd, seq);
                sendTradeEvents(cmd);
                sendMarketData(cmd);
            }
            catch (Exception ex)
            {
                //log.error("Exception when handling command result data", ex);
            }
        }


        private void sendTradeEvents(OrderCommand cmd)
        {
            MatcherTradeEvent firstEvent = cmd.MatcherEvent;
            if (firstEvent == null)
            {
                return;
            }

            if (firstEvent.EventType == MatcherEventType.REDUCE)
            {

                ReduceEvent evt = new ReduceEvent(
                        cmd.Symbol,
                        firstEvent.Size,
                        firstEvent.ActiveOrderCompleted,
                        firstEvent.Price,
                        cmd.OrderId,
                        cmd.Uid,
                        cmd.Timestamp);

                _eventsHandler.reduceEvent(evt);

                if (firstEvent.NextEvent != null)
                {
                    throw new NotImplementedException("Only single REDUCE event is expected");
                    //throw new IllegalStateException("Only single REDUCE event is expected");
                }

                return;
            }

            sendTradeEvent(cmd);
        }

        private void sendTradeEvent(OrderCommand cmd)
        {

            bool takerOrderCompleted = false;
            long mutableLong = 0L;
            List<Trade> trades = new List<Trade>();

            RejectEvent rejectEvent = null;

            cmd.processMatcherEvents(evt => {

                if (evt.EventType == MatcherEventType.TRADE)
                {

                    Trade trade = new Trade(
                            evt.MatchedOrderId,
                            evt.MatchedOrderUid,
                            evt.MatchedOrderCompleted,
                            evt.Price,
                            evt.Size);

                    trades.Add(trade);
                    mutableLong += evt.Size;

                    if (evt.ActiveOrderCompleted)
                    {
                        takerOrderCompleted = true;
                    }

                }
                else if (evt.EventType == MatcherEventType.REJECT)
                {

                    rejectEvent = new RejectEvent(
                            cmd.Symbol,
                            evt.Size,
                            evt.Price,
                            cmd.OrderId,
                            cmd.Uid,
                            cmd.Timestamp);
                }
            });

            if (trades.Count != 0)
            {
                TradeEvent evt = new TradeEvent(
                        cmd.Symbol,
                        mutableLong,
                        cmd.OrderId,
                        cmd.Uid,
                        cmd.Action,
                        takerOrderCompleted,
                        cmd.Timestamp,
                        trades);

                _eventsHandler.tradeEvent(evt);
            }

            if (rejectEvent != null) {
                _eventsHandler.rejectEvent(rejectEvent);
            }
        }

        private void sendMarketData(OrderCommand cmd)
        {
            L2MarketData marketData = cmd.MarketData;
            if (marketData != null)
            {
                List<OrderBookRecord> asks = new List<OrderBookRecord>(marketData.AskSize);
                for (int i = 0; i < marketData.AskSize; i++)
                {
                    asks.Add(new OrderBookRecord(marketData.AskPrices[i], marketData.AskVolumes[i], (int)marketData.AskOrders[i]));
                }

                List<OrderBookRecord> bids = new List<OrderBookRecord>(marketData.BidSize);
                for (int i = 0; i < marketData.BidSize; i++)
                {
                    bids.Add(new OrderBookRecord(marketData.BidPrices[i], marketData.BidVolumes[i], (int)marketData.BidOrders[i]));
                }

                _eventsHandler.orderBook(new OrderBook(cmd.Symbol, asks, bids, cmd.Timestamp));
            }
        }


        private void sendCommandResult(OrderCommand cmd, long seq)
        {

            switch (cmd.Command)
            {
                case OrderCommandType.PLACE_ORDER:
                    sendApiCommandResult(new ApiPlaceOrder(
                                    cmd.Price,
                                    cmd.Size,
                                    cmd.OrderId,
                                    cmd.Action,
                                    cmd.OrderType,
                                    cmd.Uid,
                                    cmd.Symbol,
                                    cmd.UserCookie,
                                    cmd.ReserveBidPrice),
                            cmd.ResultCode,
                            cmd.Timestamp,
                            seq);
                    break;

                case OrderCommandType.MOVE_ORDER:
                    sendApiCommandResult(new ApiMoveOrder(cmd.OrderId, cmd.Price, cmd.Uid, cmd.Symbol), cmd.ResultCode, cmd.Timestamp, seq);
                    break;

                case OrderCommandType.CANCEL_ORDER:
                    sendApiCommandResult(new ApiCancelOrder(cmd.OrderId, cmd.Uid, cmd.Symbol), cmd.ResultCode, cmd.Timestamp, seq);
                    break;

                case OrderCommandType.REDUCE_ORDER:
                    sendApiCommandResult(new ApiReduceOrder(cmd.OrderId, cmd.Uid, cmd.Symbol, cmd.Size), cmd.ResultCode, cmd.Timestamp, seq);
                    break;

                case OrderCommandType.ADD_USER:
                    sendApiCommandResult(new ApiAddUser(cmd.Uid), cmd.ResultCode, cmd.Timestamp, seq);
                    break;

                case OrderCommandType.BALANCE_ADJUSTMENT:
                    sendApiCommandResult(new ApiAdjustUserBalance(cmd.Uid, cmd.Symbol, cmd.Price, cmd.OrderId), cmd.ResultCode, cmd.Timestamp, seq);
                    break;

                case OrderCommandType.BINARY_DATA_COMMAND:
                    if (cmd.ResultCode != CommandResultCode.ACCEPTED)
                    {
                        sendApiCommandResult(new ApiBinaryDataCommand(cmd.UserCookie, null), cmd.ResultCode, cmd.Timestamp, seq);
                    }
                    break;

                case OrderCommandType.ORDER_BOOK_REQUEST:
                    sendApiCommandResult(new ApiOrderBookRequest(cmd.Symbol, (int)cmd.Size), cmd.ResultCode, cmd.Timestamp, seq);
                    break;

                    // TODO add rest of commands

            }

        }

        private void sendApiCommandResult(ApiCommand cmd, CommandResultCode resultCode, long timestamp, long seq)
        {
            cmd.Timestamp = timestamp;
            ApiCommandResult commandResult = new ApiCommandResult(cmd, resultCode, seq);
            _eventsHandler.commandResult(commandResult);
        }
    }
}
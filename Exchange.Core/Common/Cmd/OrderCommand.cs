using Exchange.Core.Common;
using Exchange.Core.Common.Cmd;
using System;
using System.Collections.Generic;

namespace Exchange.Core
{
    public class OrderCommand : IOrder
    {
        public static OrderCommandBuilder Builder()
        {
            return new OrderCommandBuilder();
        }

        public OrderCommandType Command { get; }
        public long OrderId { get; }

        public int Symbol { get; }

        public long Price { get; }
        public long Size { get; }

        // new orders INPUT - reserved price for fast moves of GTC bid orders in exchange mode
        public long ReserveBidPrice { get; }

        // required for PLACE_ORDER only;
        // for CANCEL/MOVE contains original order action (filled by orderbook)
        public OrderAction Action { get; set; }

        public OrderType OrderType { get; }

        public long Uid { get; }

        public long Timestamp { get; }
        public int UserCookie { get; }
        // filled by grouping processor:

        public long eventsGroup { get; }
        public int serviceFlags { get; }

        // result code of command execution - can also be used for saving intermediate state
        public CommandResultCode ResultCode { get; }
        // trade events chain
        public MatcherTradeEvent MatcherEvent { get; set; }
        // optional market data
        public L2MarketData MarketData { get; set; }

        public long Filled => 0;

        // sequence of last available for this command
        //public long matcherEventSequence;
        // ---- potential false sharing section ------

        public static OrderCommand newOrder(OrderType orderType, long orderId, long uid, long price, long reserveBidPrice, long size, OrderAction action)
        {
            return new OrderCommandBuilder()
                .command(OrderCommandType.PLACE_ORDER)
                .orderId(orderId)
                .uid(uid)
                .price(price)
                .reserveBidPrice(reserveBidPrice)
                .size(size)
                .action(action)
                .orderType(orderType)
                .resultCode(CommandResultCode.VALID_FOR_MATCHING_ENGINE)
                .build();
        }

        public static OrderCommand cancel(long orderId, long uid)
        {
            return new OrderCommandBuilder()
                .command(OrderCommandType.CANCEL_ORDER)
                .orderId(orderId)
                .uid(uid)
                .resultCode(CommandResultCode.VALID_FOR_MATCHING_ENGINE)
                .build();
        }

        public static OrderCommand reduce(long orderId, long uid, long reduceSize)
        {
            return new OrderCommandBuilder()
                .command(OrderCommandType.REDUCE_ORDER)
                .orderId(orderId)
                .uid(uid)
                .size(reduceSize)
                .resultCode(CommandResultCode.VALID_FOR_MATCHING_ENGINE)
                .build();
        }

        public static OrderCommand update(long orderId, long uid, long price)
        {
            return new OrderCommandBuilder()
               .command(OrderCommandType.MOVE_ORDER)
               .orderId(orderId)
               .uid(uid)
               .price(price)
               .resultCode(CommandResultCode.VALID_FOR_MATCHING_ENGINE)
               .build();
        }
        public OrderCommand()
        {

        }

        public OrderCommand(OrderCommandType command, long orderId, int symbol, long price, long size, long reserveBidPrice, OrderAction orderAction, OrderType orderType, long uid, long timestamp, int userCookie, CommandResultCode commandResultCode, L2MarketData marketData, MatcherTradeEvent matcherTradeEvent)
        {
            Command = command;
            OrderId = orderId;
            Symbol = symbol;
            Price = price;
            Size = size;
            ReserveBidPrice = reserveBidPrice;
            Action = orderAction;
            OrderType = orderType;
            Uid = uid;
            Timestamp = timestamp;
            UserCookie = userCookie;
            ResultCode = commandResultCode;
            MarketData = marketData;
            MatcherEvent = matcherTradeEvent;
        }

        /**
     * Handles full MatcherTradeEvent chain, without removing/revoking them
     *
     * @param handler - MatcherTradeEvent handler
     */
        public void processMatcherEvents(Action<MatcherTradeEvent> handler)
        {
            MatcherTradeEvent mte = MatcherEvent;
            while (mte != null)
            {
                handler(mte);
                mte = mte.NextEvent;
            }
        }

        /**
 * Produces garbage
 * For testing only !!!
 *
 * @return list of events
 */
        public List<MatcherTradeEvent> extractEvents()
        {
            List<MatcherTradeEvent> list = new List<MatcherTradeEvent>();
            processMatcherEvents(list.Add);
            return list;
        }
        public class OrderCommandBuilder
        {
            private OrderCommandType _command;
            private long _orderId;
            private int _symbol;
            private long _price;
            private long _size;
            private long _reserveBidPrice;
            private OrderAction _orderAction;
            private OrderType _orderType;
            private long _uid;
            private long _timestamp;
            private int _userCookie;
            private CommandResultCode _commandResultCode;
            private L2MarketData _marketData;
            private MatcherTradeEvent _matcherTradeEvent;


            public OrderCommandBuilder command(OrderCommandType value)
            {
                _command = value;
                return this;
            }

            public OrderCommandBuilder orderId(long value)
            {
                _orderId = value;
                return this;
            }

            public OrderCommandBuilder symbol(int value)
            {
                _symbol = value;
                return this;
            }

            public OrderCommandBuilder price(long value)
            {
                _price = value;
                return this;
            }

            public OrderCommandBuilder size(long value)
            {
                _size = value;
                return this;
            }

            public OrderCommandBuilder reserveBidPrice(long value)
            {
                _reserveBidPrice = value;
                return this;
            }

            public OrderCommandBuilder action(OrderAction value)
            {
                _orderAction = value;
                return this;
            }

            public OrderCommandBuilder orderType(OrderType value)
            {
                _orderType = value;
                return this;
            }

            public OrderCommandBuilder uid(long value)
            {
                _uid = value;
                return this;
            }

            public OrderCommandBuilder timestamp(long value)
            {
                _timestamp = value;
                return this;
            }

            public OrderCommandBuilder userCookie(int value)
            {
                _userCookie = value;
                return this;
            }

            public OrderCommandBuilder resultCode(CommandResultCode value)
            {
                _commandResultCode = value;
                return this;
            }

            public OrderCommandBuilder matcherEvent(MatcherTradeEvent value)
            {
                _matcherTradeEvent = value;
                return this;
            }

            public OrderCommandBuilder marketData(L2MarketData value)
            {
                _marketData = value;
                return this;
            }

            public OrderCommand build()
            {
                return new OrderCommand(_command, _orderId, _symbol, _price, _size, _reserveBidPrice, _orderAction, _orderType, _uid, _timestamp, _userCookie, _commandResultCode, _marketData, _matcherTradeEvent);
            }

        }
    }
}
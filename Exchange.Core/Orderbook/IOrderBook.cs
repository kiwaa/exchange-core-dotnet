using Exchange.Core.Common;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Common.Config;
using Exchange.Core.Utils;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ObjectsPool = Exchange.Core.Collections.ObjPool.NaiveObjectsPool;

namespace Exchange.Core.Orderbook
{
    public interface IOrderBook : IStateHash //WriteBytesMarshallable, 
    {

        /**
         * Process new order.
         * Depending on price specified (whether the order is marketable),
         * order will be matched to existing opposite GTC orders from the order book.
         * In case of remaining volume (order was not matched completely):
         * IOC - reject it as partially filled.
         * GTC - place as a new limit order into th order book.
         * <p>
         * Rejection chain attached in case of error (to simplify risk handling)
         *
         * @param cmd - order to match/place
         */
        void newOrder(OrderCommand cmd);

        /**
         * Cancel order completely.
         * <p>
         * fills cmd.action  with original original order action
         *
         * @param cmd - order command
         * @return MATCHING_UNKNOWN_ORDER_ID if order was not found, otherwise SUCCESS
         */
        CommandResultCode cancelOrder(OrderCommand cmd);

        /**
         * Decrease the size of the order by specific number of lots
         * <p>
         * fills cmd.action  with original  order action
         *
         * @param cmd - order command
         * @return MATCHING_UNKNOWN_ORDER_ID if order was not found, otherwise SUCCESS
         */
        CommandResultCode reduceOrder(OrderCommand cmd);

        /**
         * Move order
         * <p>
         * newPrice - new price (if 0 or same - order will not moved)
         * fills cmd.action  with original original order action
         *
         * @param cmd - order command
         * @return MATCHING_UNKNOWN_ORDER_ID if order was not found, otherwise SUCCESS
         */
        CommandResultCode moveOrder(OrderCommand cmd);

        // testing only ?
        int getOrdersNum(OrderAction action);

        // testing only ?
        long getTotalOrdersVolume(OrderAction action);

        // testing only ?
        IOrder getOrderById(long orderId);

        // testing only - validateInternalState without changing state
        void validateInternalState();

        /**
         * @return actual implementation
         */
        OrderBookImplType getImplementationType();

        /**
         * Search for all orders for specified user.<p>
         * Slow, because order book do not maintain uid-to-order index.<p>
         * Produces garbage.<p>
         * Orders must be processed before doing any other mutable call.<p>
         *
         * @param uid user id
         * @return list of orders
         */
        List<Order> findUserOrders(long uid);

        CoreSymbolSpecification getSymbolSpec();

        IEnumerable<IOrder> askOrdersStream(bool sorted);

        IEnumerable<IOrder> bidOrdersStream(bool sorted);

        /**
         * State hash for order books is implementation-agnostic
         * Look {@link IOrderBook#validateInternalState} for full internal state validation for de-serialized objects
         *
         * @return state hash code
         */
        int IStateHash.stateHash()
        {

            // log.debug("State hash of {}", orderBook.getClass().getSimpleName());
            // log.debug("  Ask orders stream: {}", orderBook.askOrdersStream(true).collect(Collectors.toList()));
            // log.debug("  Ask orders hash: {}", stateHashStream(orderBook.askOrdersStream(true)));
            // log.debug("  Bid orders stream: {}", orderBook.bidOrdersStream(true).collect(Collectors.toList()));
            // log.debug("  Bid orders hash: {}", stateHashStream(orderBook.bidOrdersStream(true)));
            // log.debug("  getSymbolSpec: {}", orderBook.getSymbolSpec());
            // log.debug("  getSymbolSpec hash: {}", orderBook.getSymbolSpec().stateHash());

            return 97 * HashingUtils.stateHashStream(askOrdersStream(true)) +
                    997 * HashingUtils.stateHashStream(bidOrdersStream(true)) +
                    9997 * getSymbolSpec().stateHash();
        }

        /**
         * Obtain current L2 Market Data snapshot
         *
         * @param size max size for each part (ask, bid)
         * @return L2 Market Data snapshot
         */
        public L2MarketData getL2MarketDataSnapshot(int size)
        {
            int asksSize = getTotalAskBuckets(size);
            int bidsSize = getTotalBidBuckets(size);
            L2MarketData data = new L2MarketData(asksSize, bidsSize);
            fillAsks(asksSize, data);
            fillBids(bidsSize, data);
            return data;
        }

        public L2MarketData getL2MarketDataSnapshot()
        {
            return getL2MarketDataSnapshot(int.MaxValue);
        }

        /**
         * Request to publish L2 market data into outgoing disruptor message
         *
         * @param data - pre-allocated object from ring buffer
         */
        public void publishL2MarketDataSnapshot(L2MarketData data)
        {
            int size = L2MarketData.L2_SIZE;
            fillAsks(size, data);
            fillBids(size, data);
        }

        void fillAsks(int size, L2MarketData data);

        void fillBids(int size, L2MarketData data);

        int getTotalAskBuckets(int limit);

        int getTotalBidBuckets(int limit);


        static CommandResultCode processCommand(IOrderBook orderBook, OrderCommand cmd)
        {
            OrderCommandType commandType = cmd.Command;

            if (commandType == OrderCommandType.MOVE_ORDER)
            {
                return orderBook.moveOrder(cmd);
            }
            else if (commandType == OrderCommandType.CANCEL_ORDER)
            {
                return orderBook.cancelOrder(cmd);
            }
            else if (commandType == OrderCommandType.REDUCE_ORDER)
            {
                return orderBook.reduceOrder(cmd);
            }
            else if (commandType == OrderCommandType.PLACE_ORDER)
            {
                if (cmd.ResultCode == CommandResultCode.VALID_FOR_MATCHING_ENGINE)
                {
                    orderBook.newOrder(cmd);
                    return CommandResultCode.SUCCESS;
                }
                else
                {
                    return cmd.ResultCode; // no change
                }

            }
            else if (commandType == OrderCommandType.ORDER_BOOK_REQUEST)
            {
                int size = (int)cmd.Size;
                cmd.MarketData = orderBook.getL2MarketDataSnapshot(size >= 0 ? size : int.MaxValue);
                return CommandResultCode.SUCCESS;

            }
            else
            {
                return CommandResultCode.MATCHING_UNSUPPORTED_COMMAND;
            }

        }

        static IOrderBook create(IBytesIn bytes, ObjectsPool objectsPool, OrderBookEventsHelper eventsHelper, LoggingConfiguration loggingCfg)
        {
            var type = (OrderBookImplType)bytes.readByte();
            switch (type)
            {
                case OrderBookImplType.NAIVE:
                    return new OrderBookNaiveImpl(bytes, loggingCfg);
                case OrderBookImplType.DIRECT:
                    return new OrderBookDirectImpl(bytes, objectsPool, eventsHelper, loggingCfg);
                default:
                    throw new InvalidOperationException();
            }
        }

        //@FunctionalInterface
        //interface OrderBookFactory
        //{

        //    IOrderBook create(CoreSymbolSpecification spec, ObjectsPool pool, OrderBookEventsHelper eventsHelper, LoggingConfiguration loggingCfg);
        //}

    }



}

using Exchange.Core.Common;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Common.Config;
using Exchange.Core.Utils;
using log4net;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ObjectsPool = Exchange.Core.Collections.ObjPool.NaiveObjectsPool;

namespace Exchange.Core.Orderbook
{
    public sealed class OrderBookNaiveImpl : IOrderBook
    {
        private class LongReverseComparer : IComparer<long>
        {
            public static readonly LongReverseComparer Instance = new LongReverseComparer();
            public int Compare([AllowNull] long x, [AllowNull] long y)
            {
                return -x.CompareTo(y);
            }
        }

        private static ILog log = LogManager.GetLogger(typeof(OrderBookNaiveImpl));

        private readonly SortedDictionary<long, OrdersBucketNaive> askBuckets;
        private readonly SortedDictionary<long, OrdersBucketNaive> bidBuckets;

        private readonly CoreSymbolSpecification symbolSpec;

        private readonly Dictionary<long, Order> idMap = new Dictionary<long, Order>();

        private readonly OrderBookEventsHelper eventsHelper;

        private readonly bool logDebug;

        public OrderBookNaiveImpl(CoreSymbolSpecification symbolSpec,
                                  ObjectsPool pool,
                                  OrderBookEventsHelper eventsHelper,
                                  LoggingConfiguration loggingCfg)
        {

            this.symbolSpec = symbolSpec;
            this.askBuckets = new SortedDictionary<long, OrdersBucketNaive>();
            this.bidBuckets = new SortedDictionary<long, OrdersBucketNaive>(LongReverseComparer.Instance);
            this.eventsHelper = eventsHelper;
            this.logDebug = loggingCfg.LoggingLevels.HasFlag(LoggingLevel.LOGGING_MATCHING_DEBUG);
        }

        public OrderBookNaiveImpl(CoreSymbolSpecification symbolSpec,
                                  LoggingConfiguration loggingCfg)
        {

            this.symbolSpec = symbolSpec;
            this.askBuckets = new SortedDictionary<long, OrdersBucketNaive>();
            this.bidBuckets = new SortedDictionary<long, OrdersBucketNaive>(LongReverseComparer.Instance);
            this.eventsHelper = OrderBookEventsHelper.NON_POOLED_EVENTS_HELPER;
            this.logDebug = loggingCfg.LoggingLevels.HasFlag(LoggingLevel.LOGGING_MATCHING_DEBUG);
        }

        public OrderBookNaiveImpl(IBytesIn bytes, LoggingConfiguration loggingCfg)
        {
            this.symbolSpec = new CoreSymbolSpecification(bytes);
            this.askBuckets = SerializationUtils.readLongMap(bytes, () => new SortedDictionary<long, OrdersBucketNaive>(), bytesIn => new OrdersBucketNaive(bytesIn));
            this.bidBuckets = SerializationUtils.readLongMap(bytes, () => new SortedDictionary<long, OrdersBucketNaive>(LongReverseComparer.Instance), bytesIn => new OrdersBucketNaive(bytesIn));

            this.eventsHelper = OrderBookEventsHelper.NON_POOLED_EVENTS_HELPER;
            // reconstruct ordersId-> Order cache
            // TODO check resulting performance
            foreach (var bucket in askBuckets.Values)
                bucket.forEachOrder(order=>idMap[order.OrderId] = order);
            foreach (var bucket in bidBuckets.Values)
                bucket.forEachOrder(order => idMap[order.OrderId] = order);

            this.logDebug = loggingCfg.LoggingLevels.HasFlag(LoggingLevel.LOGGING_MATCHING_DEBUG);
            //validateInternalState();
        }

        public void newOrder(OrderCommand cmd)
        {

            switch (cmd.OrderType)
            {
                case OrderType.GTC:
                    newOrderPlaceGtc(cmd);
                    break;
                case OrderType.IOC:
                    newOrderMatchIoc(cmd);
                    break;
                case OrderType.FOK_BUDGET:
                    newOrderMatchFokBudget(cmd);
                    break;
                // TODO IOC_BUDGET and FOK support
                default:
                    log.Warn($"Unsupported order type: {cmd}");
                    eventsHelper.attachRejectEvent(cmd, cmd.Size);
                    break;
            }
        }

        private void newOrderPlaceGtc(OrderCommand cmd)
        {

            OrderAction action = cmd.Action;
            long price = cmd.Price;
            long size = cmd.Size;

            // check if order is marketable (if there are opposite matching orders)
            long filledSize = tryMatchInstantly(cmd, action, price, 0, cmd);
            if (filledSize == size)
            {
                // order was matched completely - nothing to place - can just return
                return;
            }

            long newOrderId = cmd.OrderId;
            if (idMap.ContainsKey(newOrderId))
            {
                // duplicate order id - can match, but can not place
                eventsHelper.attachRejectEvent(cmd, cmd.Size - filledSize);
                log.Warn($"duplicate order id: {cmd}");
                return;
            }

            // normally placing regular GTC limit order
            Order orderRecord = new Order(
                    newOrderId,
                    price,
                    size,
                    filledSize,
                    cmd.ReserveBidPrice,
                    action,
                    cmd.Uid,
                    cmd.Timestamp);

            var map = getBucketsByAction(action);
            if (!map.TryGetValue(price, out OrdersBucketNaive value))
            {
                value = new OrdersBucketNaive(price);
                map.Add(price, value);
            }
            value.put(orderRecord);
                    //.computeIfAbsent(price, OrdersBucketNaive::new)
                    //.put(orderRecord);

            idMap[newOrderId] = orderRecord;
        }

        private void newOrderMatchIoc(OrderCommand cmd)
        {

            long filledSize = tryMatchInstantly(cmd, cmd.Action, cmd.Price, 0, cmd);

            long rejectedSize = cmd.Size - filledSize;

            if (rejectedSize != 0)
            {
                // was not matched completely - send reject for not-completed IoC order
                eventsHelper.attachRejectEvent(cmd, rejectedSize);
            }
        }

        private void newOrderMatchFokBudget(OrderCommand cmd)
        {

            long size = cmd.Size;

            SortedDictionary<long, OrdersBucketNaive> subtreeForMatching =
                    cmd.Action == OrderAction.ASK ? bidBuckets : askBuckets;

            long? budget = checkBudgetToFill(size, subtreeForMatching);

            if (logDebug) log.Debug($"Budget calc: {budget} requested: {cmd.Price}");

            if (budget.HasValue && isBudgetLimitSatisfied(cmd.Action, budget.Value, cmd.Price))
            {
                tryMatchInstantly(cmd, cmd.Action, null, 0, cmd);
            }
            else
            {
                eventsHelper.attachRejectEvent(cmd, size);
            }
        }

        private bool isBudgetLimitSatisfied(OrderAction orderAction, long calculated, long limit)
        {
            return calculated == limit || (orderAction == OrderAction.BID ^ calculated > limit);
        }


        private long? checkBudgetToFill(
                long size,
                SortedDictionary<long, OrdersBucketNaive> matchingBuckets)
        {

            long budget = 0;

            foreach (var bucket in matchingBuckets.Values)
            {

                long availableSize = bucket.TotalVolume;
                long price = bucket.Price;

                if (size > availableSize)
                {
                    size -= availableSize;
                    budget += availableSize * price;
                    if (logDebug) log.Debug($"add    {price} * {availableSize} -> {budget}");
                }
                else
                {
                    long result = budget + size * price;
                    if (logDebug) log.Debug($"return {price} * {size} -> {result}");
                    return result;
                }
            }
            if (logDebug) log.Debug($"not enough liquidity to fill size={size}");
            return null;
        }

        //private SortedDictionary<long, OrdersBucketNaive> subtreeForMatching(OrderAction action, long price)
        //{
        //    var map = (action == OrderAction.ASK ? bidBuckets : askBuckets);
        //    return map; // will it work at all?
        //           // .headMap(price, true);
        //}

        /**
         * Match the order instantly to specified sorted buckets map
         * Fully matching orders are removed from orderId index
         * Should any trades occur - they sent to tradesConsumer
         *
         * @param activeOrder     - GTC or IOC order to match
         * @param matchingBuckets - sorted buckets map
         * @param filled          - current 'filled' value for the order
         * @param triggerCmd      - triggered command (taker)
         * @return new filled size
         */
        private long tryMatchInstantly(
                IOrder activeOrder,
                OrderAction orderAction,
                long? orderPrice,
//                SortedDictionary<long, OrdersBucketNaive> matchingBuckets,
                long filled,
                OrderCommand triggerCmd)
        {

            //        log.info("matchInstantly: {} {}", order, matchingBuckets);
            var matchingBuckets = (orderAction == OrderAction.ASK ? bidBuckets : askBuckets);

            if (matchingBuckets.Count == 0)
            {
                return filled;
            }

            long orderSize = activeOrder.Size;

            MatcherTradeEvent eventsTail = null;

            List<long> emptyBuckets = new List<long>();
            foreach (var pair in matchingBuckets)
            {
                // headMap sim
                if (orderPrice.HasValue && 
                    ((orderAction == OrderAction.ASK && pair.Key < orderPrice) ||
                    (orderAction == OrderAction.BID && pair.Key > orderPrice)))
                {
                    break;
                }
                    //            log.debug("Matching bucket: {} ...", bucket);
                    //            log.debug("... with order: {}", activeOrder);

                    var bucket = pair.Value;
                long sizeLeft = orderSize - filled;

                OrdersBucketNaive.MatcherResult bucketMatchings = bucket.match(sizeLeft, activeOrder, eventsHelper);

                foreach (var remove in bucketMatchings.OrdersToRemove)
                    idMap.Remove(remove);

                filled += bucketMatchings.Volume;

                // attach chain received from bucket matcher
                if (eventsTail == null)
                {
                    triggerCmd.MatcherEvent = bucketMatchings.EventsChainHead;
                }
                else
                {
                    eventsTail.NextEvent = bucketMatchings.EventsChainHead;
                }
                eventsTail = bucketMatchings.EventsChainTail;

                //            log.debug("Matching orders: {}", matchingOrders);
                //            log.debug("order.filled: {}", activeOrder.filled);

                long price = bucket.Price;

                // remove empty buckets
                if (bucket.TotalVolume == 0)
                {
                    emptyBuckets.Add(price);
                }

                if (filled == orderSize)
                {
                    // enough matched
                    break;
                }
            }

            // remove empty buckets (is it necessary?)
            // TODO can remove through iterator ??
            foreach (var p in emptyBuckets)
                matchingBuckets.Remove(p);

            //        log.debug("emptyBuckets: {}", emptyBuckets);
            //        log.debug("matchingRecords: {}", matchingRecords);

            return filled;
        }

        /**
         * Remove an order.<p>
         *
         * @param cmd cancel command (orderId - order to remove)
         * @return true if order removed, false if not found (can be removed/matched earlier)
         */
        public CommandResultCode cancelOrder(OrderCommand cmd)
        {
            long orderId = cmd.OrderId;

            if (!idMap.TryGetValue(orderId, out Order order) || order.Uid != cmd.Uid)
            {
                // order already matched and removed from order book previously
                return CommandResultCode.MATCHING_UNKNOWN_ORDER_ID;
            }

            // now can remove it
            idMap.Remove(orderId);

            SortedDictionary<long, OrdersBucketNaive> buckets = getBucketsByAction(order.Action);
            long price = order.Price;
            if (!buckets.TryGetValue(price, out OrdersBucketNaive ordersBucket))
            {
                // not possible state
                throw new InvalidOperationException("Can not find bucket for order price=" + price + " for order " + order);
            }

            // remove order and whole bucket if its empty
            ordersBucket.remove(orderId, cmd.Uid);
            if (ordersBucket.TotalVolume == 0)
            {
                buckets.Remove(price);
            }

            // send reduce event
            cmd.MatcherEvent = eventsHelper.sendReduceEvent(order, order.Size - order.Filled, true);

            // fill action fields (for events handling)
            cmd.Action = order.Action;

            return CommandResultCode.SUCCESS;
        }

        public CommandResultCode reduceOrder(OrderCommand cmd)
        {
            long orderId = cmd.OrderId;
            long requestedReduceSize = cmd.Size;

            if (requestedReduceSize <= 0)
            {
                return CommandResultCode.MATCHING_REDUCE_FAILED_WRONG_SIZE;
            }

            if (!idMap.TryGetValue(orderId, out Order order) || order.Uid != cmd.Uid)
            {
                // order already matched and removed from order book previously
                return CommandResultCode.MATCHING_UNKNOWN_ORDER_ID;
            }

            long remainingSize = order.Size - order.Filled;
            long reduceBy = Math.Min(remainingSize, requestedReduceSize);

            SortedDictionary<long, OrdersBucketNaive> buckets = getBucketsByAction(order.Action);
            if (!buckets.TryGetValue(order.Price, out OrdersBucketNaive ordersBucket))
            {
                // not possible state
                throw new InvalidOperationException("Can not find bucket for order price=" + order.Price + " for order " + order);
            }

            bool canRemove = (reduceBy == remainingSize);

            if (canRemove)
            {

                // now can remove order
                idMap.Remove(orderId);

                // canRemove order and whole bucket if it is empty
                ordersBucket.remove(orderId, cmd.Uid);
                if (ordersBucket.TotalVolume == 0)
                {
                    buckets.Remove(order.Price);
                }

            }
            else
            {

                order.Size -= reduceBy;
                ordersBucket.reduceSize(reduceBy);
            }

            // send reduce event
            cmd.MatcherEvent = eventsHelper.sendReduceEvent(order, reduceBy, canRemove);

            // fill action fields (for events handling)
            cmd.Action = order.Action;

            return CommandResultCode.SUCCESS;
        }

        public CommandResultCode moveOrder(OrderCommand cmd)
        {
            long orderId = cmd.OrderId;
            long newPrice = cmd.Price;

            if (!idMap.TryGetValue(orderId, out Order order) || order.Uid != cmd.Uid)
            {
                // order already matched and removed from order book previously
                return CommandResultCode.MATCHING_UNKNOWN_ORDER_ID;
            }

            long price = order.Price;
            SortedDictionary<long, OrdersBucketNaive> buckets = getBucketsByAction(order.Action);
            OrdersBucketNaive bucket = buckets[price];

            // fill action fields (for events handling)
            cmd.Action = order.Action;

            // reserved price risk check for exchange bids
            if (symbolSpec.Type == SymbolType.CURRENCY_EXCHANGE_PAIR && order.Action == OrderAction.BID && cmd.Price > order.ReserveBidPrice)
            {
                return CommandResultCode.MATCHING_MOVE_FAILED_PRICE_OVER_RISK_LIMIT;
            }

            // take order out of the original bucket and clean bucket if its empty
            bucket.remove(orderId, cmd.Uid);

            if (bucket.TotalVolume == 0)
            {
                buckets.Remove(price);
            }

            order.Price = newPrice;

            // try match with new price
            //SortedDictionary<long, OrdersBucketNaive> matchingArea = subtreeForMatching(order.Action, newPrice);
            long filled = tryMatchInstantly(order, order.Action, newPrice, order.Filled, cmd);
            if (filled == order.Size)
            {
                // order was fully matched (100% marketable) - removing from order book
                idMap.Remove(orderId);
                return CommandResultCode.SUCCESS;
            }
            order.Filled = filled;

            // if not filled completely - put it into corresponding bucket
            if (!buckets.TryGetValue(newPrice, out OrdersBucketNaive anotherBucket))
            {
                anotherBucket = new OrdersBucketNaive(newPrice);
                buckets.Add(newPrice, anotherBucket);                
            }
            anotherBucket.put(order);
            //OrdersBucketNaive anotherBucket = buckets.computeIfAbsent(newPrice, p=> {
            //    OrdersBucketNaive b = new OrdersBucketNaive(p);
            //    return b;
            //});
            //anotherBucket.put(order);

            return CommandResultCode.SUCCESS;
        }

        /**
         * Get bucket by order action
         *
         * @param action - action
         * @return bucket - navigable map
         */
        private SortedDictionary<long, OrdersBucketNaive> getBucketsByAction(OrderAction action)
        {
            return action == OrderAction.ASK ? askBuckets : bidBuckets;
        }


        /**
         * Get order from internal map
         *
         * @param orderId - order Id
         * @return order from map
         */
        public IOrder getOrderById(long orderId)
        {
            return idMap.TryGetValue(orderId, out Order order) ? order : null;
//            return idMap[orderId];
        }

        public void fillAsks(int size, L2MarketData data)
        {
            if (size == 0)
            {
                data.AskSize = 0;
                return;
            }

            int i = 0;
            foreach (var bucket in askBuckets.Values)
            {
                data.AskPrices[i] = bucket.Price;
                data.AskVolumes[i] = bucket.TotalVolume;
                data.AskOrders[i] = bucket.getNumOrders();
                if (++i == size)
                {
                    break;
                }
            }
            data.AskSize = i;
        }

        public void fillBids(int size, L2MarketData data)
        {
            if (size == 0)
            {
                data.BidSize = 0;
                return;
            }

            int i = 0;
            foreach (var bucket in bidBuckets.Values)
            {
                data.BidPrices[i] = bucket.Price;
                data.BidVolumes[i] = bucket.TotalVolume;
                data.BidOrders[i] = bucket.getNumOrders();
                if (++i == size)
                {
                    break;
                }
            }
            data.BidSize = i;
        }

        public int getTotalAskBuckets(int limit)
        {
            return Math.Min(limit, askBuckets.Count);
        }

        public int getTotalBidBuckets(int limit)
        {
            return Math.Min(limit, bidBuckets.Count);
        }
        
        public void validateInternalState()
        {
            foreach (var v in askBuckets.Values)
                v.validate();
            foreach (var v in bidBuckets.Values)
                v.validate();
        }

        public OrderBookImplType getImplementationType()
        {
            return OrderBookImplType.NAIVE;
        }

        public List<Order> findUserOrders(long uid)
        {
            List<Order> list = new List<Order>();
            Action<OrdersBucketNaive> bucketConsumer = bucket=>bucket.forEachOrder(order => {
                if (order.Uid == uid)
                {
                    list.Add(order);
                }
            });
            foreach (var v in askBuckets.Values)
                bucketConsumer(v);
            foreach (var v in bidBuckets.Values)
                bucketConsumer(v);
            return list;
        }

        public CoreSymbolSpecification getSymbolSpec()
        {
            return symbolSpec;
        }

        public IEnumerable<IOrder> askOrdersStream(bool sorted)
        {
            return askBuckets.Values.SelectMany(bucket => bucket.getAllOrders());
        }

        public IEnumerable<IOrder> bidOrdersStream(bool sorted)
        {
            return bidBuckets.Values.SelectMany(bucket => bucket.getAllOrders());
        }

        // for testing only
        public int getOrdersNum(OrderAction action)
        {
            SortedDictionary<long, OrdersBucketNaive> buckets = action == OrderAction.ASK ? askBuckets : bidBuckets;
            return buckets.Values.Select(x => x.getNumOrders()).Sum();
            //        int askOrders = askBuckets.values().stream().mapToInt(OrdersBucketNaive::getNumOrders).sum();
            //        int bidOrders = bidBuckets.values().stream().mapToInt(OrdersBucketNaive::getNumOrders).sum();
            //log.debug("idMap:{} askOrders:{} bidOrders:{}", idMap.size(), askOrders, bidOrders);
            //        int knownOrders = idMap.size();
            //        assert knownOrders == askOrders + bidOrders : "inconsistent known orders";
        }

        public long getTotalOrdersVolume(OrderAction action)
        {
            SortedDictionary<long, OrdersBucketNaive> buckets = action == OrderAction.ASK ? askBuckets : bidBuckets;
            return buckets.Values.Select(x => x.TotalVolume).Sum();
        }


        //public void writeMarshallable(BytesOut bytes)
        //{
        //    bytes.writeByte(getImplementationType().getCode());
        //    symbolSpec.writeMarshallable(bytes);
        //    SerializationUtils.marshallLongMap(askBuckets, bytes);
        //    SerializationUtils.marshallLongMap(bidBuckets, bytes);
        //}
    }

}

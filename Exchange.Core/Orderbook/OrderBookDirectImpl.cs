using Exchange.Core.Collections;
using Exchange.Core.Collections.ObjPool;
using Exchange.Core.Common;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Common.Config;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ObjectsPool = Exchange.Core.Collections.ObjPool.NaiveObjectsPool;

namespace Exchange.Core.Orderbook
{
    public sealed class OrderBookDirectImpl : IOrderBook
    {
        private static ILog log = LogManager.GetLogger(typeof(OrderBookDirectImpl));

        // buckets
        private readonly LongAdaptiveRadixTreeMap<Bucket> askPriceBuckets;
        private readonly LongAdaptiveRadixTreeMap<Bucket> bidPriceBuckets;

        // symbol specification
        private readonly CoreSymbolSpecification symbolSpec;

        // index: orderId -> order
        private readonly LongAdaptiveRadixTreeMap<DirectOrder> orderIdIndex;
        //private final Long2ObjectHashMap<DirectOrder> orderIdIndex = new Long2ObjectHashMap<>();
        //private final LongObjectHashMap<DirectOrder> orderIdIndex = new LongObjectHashMap<>();

        // heads (nullable)
        private DirectOrder bestAskOrder = null;
        private DirectOrder bestBidOrder = null;

        // Object pools
        private readonly ObjectsPool objectsPool;

        private readonly OrderBookEventsHelper eventsHelper;

        private readonly bool logDebug;

        public OrderBookDirectImpl(CoreSymbolSpecification symbolSpec,
                                   ObjectsPool objectsPool,
                                   OrderBookEventsHelper eventsHelper,
                                   LoggingConfiguration loggingCfg)
        {

            this.symbolSpec = symbolSpec;
            this.objectsPool = objectsPool;
            this.askPriceBuckets = new LongAdaptiveRadixTreeMap<Bucket>(objectsPool);
            this.bidPriceBuckets = new LongAdaptiveRadixTreeMap<Bucket>(objectsPool);
            this.eventsHelper = eventsHelper;
            this.orderIdIndex = new LongAdaptiveRadixTreeMap<DirectOrder>(objectsPool);
            this.logDebug = loggingCfg.LoggingLevels.HasFlag(LoggingLevel.LOGGING_MATCHING_DEBUG);
        }

        //public OrderBookDirectImpl(final BytesIn bytes,
        //                           final ObjectsPool objectsPool,
        //                           final OrderBookEventsHelper eventsHelper,
        //                           final LoggingConfiguration loggingCfg)
        //{

        //    this.symbolSpec = new CoreSymbolSpecification(bytes);
        //    this.objectsPool = objectsPool;
        //    this.askPriceBuckets = new LongAdaptiveRadixTreeMap<>(objectsPool);
        //    this.bidPriceBuckets = new LongAdaptiveRadixTreeMap<>(objectsPool);
        //    this.eventsHelper = eventsHelper;
        //    this.orderIdIndex = new LongAdaptiveRadixTreeMap<>(objectsPool);
        //    this.logDebug = loggingCfg.getLoggingLevels().contains(LoggingConfiguration.LoggingLevel.LOGGING_MATCHING_DEBUG);

        //    final int size = bytes.readInt();
        //    for (int i = 0; i < size; i++)
        //    {
        //        DirectOrder order = new DirectOrder(bytes);
        //        insertOrder(order, null);
        //        orderIdIndex.put(order.orderId, order);
        //    }
        //}

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
            long size = cmd.Size;

            // check if order is marketable there are matching orders
            long filledSize = tryMatchInstantly(cmd, cmd);
            if (filledSize == size)
            {
                // completed before being placed - can just return
                return;
            }

            long orderId = cmd.OrderId;
            // TODO eliminate double hashtable lookup?
            if (orderIdIndex.get(orderId) != null)
            { // containsKey for hashtable
              // duplicate order id - can match, but can not place
                eventsHelper.attachRejectEvent(cmd, size - filledSize);
                log.Warn($"duplicate order id: {cmd}");
                return;
            }

            long price = cmd.Price;

            // normally placing regular GTC order
            DirectOrder orderRecord = objectsPool.get(ObjectsPool.DIRECT_ORDER, x => new DirectOrder());// (Supplier<DirectOrder>)DirectOrder::new);

            orderRecord.OrderId = orderId;
            orderRecord.Price = price;
            orderRecord.Size = size;
            orderRecord.ReserveBidPrice = cmd.ReserveBidPrice;
            orderRecord.Action = cmd.Action;
            orderRecord.Uid = cmd.Uid;
            orderRecord.Timestamp = cmd.Timestamp;
            orderRecord.Filled = filledSize;

            orderIdIndex.put(orderId, orderRecord);
            insertOrder(orderRecord, null);
        }

        private void newOrderMatchIoc(OrderCommand cmd)
        {
            long filledSize = tryMatchInstantly(cmd, cmd);
            long rejectedSize = cmd.Size - filledSize;

            if (rejectedSize != 0)
            {
                // was not matched completely - send reject for not-completed IoC order
                eventsHelper.attachRejectEvent(cmd, rejectedSize);
            }
        }

        private void newOrderMatchFokBudget(OrderCommand cmd)
        {
            long budget = checkBudgetToFill(cmd.Action, cmd.Size);

            if (logDebug) log.Debug($"Budget calc: {budget} requested: {cmd.Price}");

            if (isBudgetLimitSatisfied(cmd.Action, budget, cmd.Price))
            {
                tryMatchInstantly(cmd, cmd);
            }
            else
            {
                eventsHelper.attachRejectEvent(cmd, cmd.Size);
            }
        }

        private bool isBudgetLimitSatisfied(OrderAction orderAction, long calculated, long limit)
        {
            return calculated != long.MaxValue
                    && (calculated == limit || (orderAction == OrderAction.BID ^ calculated > limit));
        }

        private long checkBudgetToFill(OrderAction action, long size)
        {
            DirectOrder makerOrder = (action == OrderAction.BID) ? bestAskOrder : bestBidOrder;

            long budget = 0L;

            // iterate through all orders
            while (makerOrder != null)
            {
                Bucket bucket = makerOrder.Parent;

                long availableSize = bucket.volume;
                long price = makerOrder.Price;

                if (size > availableSize)
                {
                    size -= availableSize;
                    budget += availableSize * price;
                    if (logDebug) log.Debug($"add    {price} * {availableSize} -> {budget}");
                }
                else
                {
                    if (logDebug) log.Debug($"return {price} * {size} -> {budget + size * price}");
                    return budget + size * price;
                }

                // switch to next order (can be null)
                makerOrder = bucket.tail.Prev;
            }
            if (logDebug) log.Debug($"not enough liquidity to fill size={size}");
            return long.MaxValue;
        }


        private long tryMatchInstantly(IOrder takerOrder,
                                       OrderCommand triggerCmd)
        {
            bool isBidAction = takerOrder.Action == OrderAction.BID;
            long limitPrice = (triggerCmd.Command == OrderCommandType.PLACE_ORDER && triggerCmd.OrderType == OrderType.FOK_BUDGET && !isBidAction)
                    ? 0L
                    : takerOrder.Price;

            DirectOrder makerOrder;
            if (isBidAction)
            {
                makerOrder = bestAskOrder;
                if (makerOrder == null || makerOrder.Price > limitPrice)
                {
                    return takerOrder.Filled;
                }
            }
            else
            {
                makerOrder = bestBidOrder;
                if (makerOrder == null || makerOrder.Price < limitPrice)
                {
                    return takerOrder.Filled;
                }
            }

            long remainingSize = takerOrder.Size - takerOrder.Filled;

            if (remainingSize == 0)
            {
                return takerOrder.Filled;
            }

            DirectOrder priceBucketTail = makerOrder.Parent.tail;

            long takerReserveBidPrice = takerOrder.ReserveBidPrice;
            //        final long takerOrderTimestamp = takerOrder.getTimestamp();

            //        log.Debug("MATCHING taker: {} remainingSize={}", takerOrder, remainingSize);

            MatcherTradeEvent eventsTail = null;

            // iterate through all orders
            do
            {

                //            log.Debug("  matching from maker order: {}", makerOrder);

                // calculate exact volume can fill for this order
                long tradeSize = Math.Min(remainingSize, makerOrder.Size - makerOrder.Filled);
                //                log.Debug("  tradeSize: {} MIN(remainingSize={}, makerOrder={})", tradeSize, remainingSize, makerOrder.size - makerOrder.filled);

                makerOrder.Filled += tradeSize;
                makerOrder.Parent.volume -= tradeSize;
                remainingSize -= tradeSize;

                // remove from order book filled orders
                bool makerCompleted = makerOrder.Size == makerOrder.Filled;
                if (makerCompleted)
                {
                    makerOrder.Parent.numOrders--;
                }

                MatcherTradeEvent tradeEvent = eventsHelper.sendTradeEvent(makerOrder, makerCompleted, remainingSize == 0, tradeSize,
                        isBidAction ? takerReserveBidPrice : makerOrder.ReserveBidPrice);

                if (eventsTail == null)
                {
                    triggerCmd.MatcherEvent = tradeEvent;
                }
                else
                {
                    eventsTail.NextEvent = tradeEvent;
                }
                eventsTail = tradeEvent;

                if (!makerCompleted)
                {
                    // maker not completed -> no unmatched volume left, can exit matching loop
                    //                    log.Debug("  not completed, exit");
                    break;
                }

                // if completed can remove maker order
                orderIdIndex.remove(makerOrder.OrderId);
                objectsPool.Put(ObjectsPool.DIRECT_ORDER, makerOrder);


                if (makerOrder == priceBucketTail)
                {
                    // reached current price tail -> remove bucket reference
                    LongAdaptiveRadixTreeMap<Bucket> buckets = isBidAction ? askPriceBuckets : bidPriceBuckets;
                    buckets.remove(makerOrder.Price);
                    objectsPool.Put(ObjectsPool.DIRECT_BUCKET, makerOrder.Parent);
                    //                log.Debug("  removed price bucket for {}", makerOrder.price);

                    // set next price tail (if there is next price)
                    if (makerOrder.Prev != null)
                    {
                        priceBucketTail = makerOrder.Prev.Parent.tail;
                    }
                }

                // switch to next order
                makerOrder = makerOrder.Prev; // can be null

            } while (makerOrder != null
                    && remainingSize > 0
                    && (isBidAction ? makerOrder.Price <= limitPrice : makerOrder.Price >= limitPrice));

            // break chain after last order
            if (makerOrder != null)
            {
                makerOrder.Next = null;
            }

            //        log.Debug("makerOrder = {}", makerOrder);
            //        log.Debug("makerOrder.parent = {}", makerOrder != null ? makerOrder.parent : null);

            // update best orders reference
            if (isBidAction)
            {
                bestAskOrder = makerOrder;
            }
            else
            {
                bestBidOrder = makerOrder;
            }

            // return filled amount
            return takerOrder.Size - remainingSize;
        }

        public CommandResultCode cancelOrder(OrderCommand cmd)
        {

            // TODO avoid double lookup ?
            DirectOrder order = orderIdIndex.get(cmd.OrderId);
            if (order == null || order.Uid != cmd.Uid)
            {
                return CommandResultCode.MATCHING_UNKNOWN_ORDER_ID;
            }
            orderIdIndex.remove(cmd.OrderId);
            objectsPool.Put(ObjectsPool.DIRECT_ORDER, order);

            Bucket freeBucket = removeOrder(order);
            if (freeBucket != null)
            {
                objectsPool.Put(ObjectsPool.DIRECT_BUCKET, freeBucket);
            }

            // fill action fields (for events handling)
            cmd.Action = order.Action;

            cmd.MatcherEvent = eventsHelper.sendReduceEvent(order, order.Size - order.Filled, true);

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

            DirectOrder order = orderIdIndex.get(orderId);
            if (order == null || order.Uid != cmd.Uid)
            {
                return CommandResultCode.MATCHING_UNKNOWN_ORDER_ID;
            }

            long remainingSize = order.Size - order.Filled;
            long reduceBy = Math.Min(remainingSize, requestedReduceSize);
            bool canRemove = reduceBy == remainingSize;

            if (canRemove)
            {

                orderIdIndex.remove(orderId);
                objectsPool.Put(ObjectsPool.DIRECT_ORDER, order);

                Bucket freeBucket = removeOrder(order);
                if (freeBucket != null)
                {
                    objectsPool.Put(ObjectsPool.DIRECT_BUCKET, freeBucket);
                }

            }
            else
            {
                order.Size -= reduceBy;
                order.Parent.volume -= reduceBy;
            }

            cmd.MatcherEvent = eventsHelper.sendReduceEvent(order, reduceBy, canRemove);

            // fill action fields (for events handling)
            cmd.Action = order.Action;

            return CommandResultCode.SUCCESS;
        }

        public CommandResultCode moveOrder(OrderCommand cmd)
        {

            // order lookup
            DirectOrder orderToMove = orderIdIndex.get(cmd.OrderId);
            if (orderToMove == null || orderToMove.Uid != cmd.Uid)
            {
                return CommandResultCode.MATCHING_UNKNOWN_ORDER_ID;
            }

            // risk check for exchange bids
            if (symbolSpec.Type == SymbolType.CURRENCY_EXCHANGE_PAIR && orderToMove.Action == OrderAction.BID && cmd.Price > orderToMove.ReserveBidPrice)
            {
                return CommandResultCode.MATCHING_MOVE_FAILED_PRICE_OVER_RISK_LIMIT;
            }

            // remove order
            Bucket freeBucket = removeOrder(orderToMove);

            // update price
            orderToMove.Price = cmd.Price;

            // fill action fields (for events handling)
            cmd.Action = orderToMove.Action;

            // try match with new price as a taker order
            long filled = tryMatchInstantly(orderToMove, cmd);
            if (filled == orderToMove.Size)
            {
                // order was fully matched - removing
                orderIdIndex.remove(cmd.OrderId);
                // returning free object back to the pool
                objectsPool.Put(ObjectsPool.DIRECT_ORDER, orderToMove);
                return CommandResultCode.SUCCESS;
            }

            // not filled completely, inserting into new position
            orderToMove.Filled = filled;

            // insert into a new place
            insertOrder(orderToMove, freeBucket);

            return CommandResultCode.SUCCESS;
        }


        private Bucket removeOrder(DirectOrder order)
        {

            Bucket bucket = order.Parent;
            bucket.volume -= order.Size - order.Filled;
            bucket.numOrders--;
            Bucket bucketRemoved = null;

            if (bucket.tail == order)
            {
                // if we removing tail order -> change bucket tail reference
                if (order.Next == null || order.Next.Parent != bucket)
                {
                    // if no next or next order has different parent -> then it was the last bucket -> remove record
                    LongAdaptiveRadixTreeMap<Bucket> buckets = order.Action == OrderAction.ASK ? askPriceBuckets : bidPriceBuckets;
                    buckets.remove(order.Price);
                    bucketRemoved = bucket;
                }
                else
                {
                    // otherwise at least one order always having the same parent left -> update tail reference to it
                    bucket.tail = order.Next; // always not null
                }
            }

            // update neighbor orders
            if (order.Next != null)
            {
                order.Next.Prev = order.Prev; // can be null
            }
            if (order.Prev != null)
            {
                order.Prev.Next = order.Next; // can be null
            }

            // check if best ask/bid were referring to the order we just removed
            if (order == bestAskOrder)
            {
                bestAskOrder = order.Prev; // can be null
            }
            else if (order == bestBidOrder)
            {
                bestBidOrder = order.Prev; // can be null
            }

            return bucketRemoved;
        }


        private void insertOrder(DirectOrder order, Bucket freeBucket)
        {

            //        log.Debug("   + insert order: {}", order);

            bool isAsk = order.Action == OrderAction.ASK;
            LongAdaptiveRadixTreeMap<Bucket> buckets = isAsk ? askPriceBuckets : bidPriceBuckets;
            Bucket toBucket = buckets.get(order.Price);

            if (toBucket != null)
            {
                // update tail if bucket already exists
                //            log.Debug(">>>> increment bucket {} from {} to {}", toBucket.tail.price, toBucket.volume, toBucket.volume +  order.size - order.filled);

                // can put bucket back to the pool (because target bucket already exists)
                if (freeBucket != null)
                {
                    objectsPool.Put(ObjectsPool.DIRECT_BUCKET, freeBucket);
                }

                toBucket.volume += order.Size - order.Filled;
                toBucket.numOrders++;
                DirectOrder oldTail = toBucket.tail; // always exists, not null
                DirectOrder prevOrder = oldTail.Prev; // can be null
                                                      // update neighbors
                toBucket.tail = order;
                oldTail.Prev = order;
                if (prevOrder != null)
                {
                    prevOrder.Next = order;
                }
                // update self
                order.Next = oldTail;
                order.Prev = prevOrder;
                order.Parent = toBucket;

            }
            else
            {

                // insert a new bucket (reuse existing)
                Bucket newBucket = freeBucket != null
                        ? freeBucket
                        : objectsPool.get(ObjectsPool.DIRECT_BUCKET, x => new Bucket());

                newBucket.tail = order;
                newBucket.volume = order.Size - order.Filled;
                newBucket.numOrders = 1;
                order.Parent = newBucket;
                buckets.put(order.Price, newBucket);
                Bucket lowerBucket = isAsk ? buckets.getLowerValue(order.Price) : buckets.getHigherValue(order.Price);
                if (lowerBucket != null)
                {
                    // attache new bucket and event to the lower entry
                    DirectOrder lowerTail = lowerBucket.tail;
                    DirectOrder prevOrder = lowerTail.Prev; // can be null
                                                            // update neighbors
                    lowerTail.Prev = order;
                    if (prevOrder != null)
                    {
                        prevOrder.Next = order;
                    }
                    // update self
                    order.Next = lowerTail;
                    order.Prev = prevOrder;
                }
                else
                {

                    // if no floor entry, then update best order
                    DirectOrder oldBestOrder = isAsk ? bestAskOrder : bestBidOrder; // can be null

                    if (oldBestOrder != null)
                    {
                        oldBestOrder.Next = order;
                    }

                    if (isAsk)
                    {
                        bestAskOrder = order;
                    }
                    else
                    {
                        bestBidOrder = order;
                    }

                    // update self
                    order.Next = null;
                    order.Prev = oldBestOrder;
                }
            }
        }

        public int getOrdersNum(OrderAction action)
        {
            LongAdaptiveRadixTreeMap<Bucket> buckets = action == OrderAction.ASK ? askPriceBuckets : bidPriceBuckets;
            int accum = 0;
            buckets.forEach((p, b) => accum += b.numOrders, int.MaxValue);
            return accum;
        }

        public long getTotalOrdersVolume(OrderAction action)
        {
            LongAdaptiveRadixTreeMap<Bucket> buckets = action == OrderAction.ASK ? askPriceBuckets : bidPriceBuckets;
            long accum = 0;
            buckets.forEach((p, b) => accum += b.volume, int.MaxValue);
            return accum;
        }

        public IOrder getOrderById(long orderId)
        {
            return orderIdIndex.get(orderId);
        }

        public void validateInternalState()
        {
            //Long2ObjectHashMap<DirectOrder> ordersInChain = new Long2ObjectHashMap<>(orderIdIndex.size(int.MaxValue), 0.8f);
            Dictionary<long, DirectOrder> ordersInChain = new Dictionary<long, DirectOrder>(orderIdIndex.size(int.MaxValue));
            validateChain(true, ordersInChain);
            validateChain(false, ordersInChain);
            //        log.Debug("ordersInChain={}", ordersInChain);
            //        log.Debug("orderIdIndex={}", orderIdIndex);

            //        log.Debug("orderIdIndex.keySet()={}", orderIdIndex.keySet().toSortedArray());
            //        log.Debug("ordersInChain=        {}", ordersInChain.toSortedArray());
            orderIdIndex.forEach((k, v) =>
            {
                if (ordersInChain.Remove(k) != true)
                {
                    thrw("chained orders does not contain orderId=" + k);
                }
            }, int.MaxValue);

            if (ordersInChain.Count != 0)
            {
                thrw("orderIdIndex does not contain each order from chains");
            }
        }

        private void validateChain(bool asksChain, Dictionary<long, DirectOrder> ordersInChain)
        {

            // buckets index
            LongAdaptiveRadixTreeMap<Bucket> buckets = asksChain ? askPriceBuckets : bidPriceBuckets;
            //LongObjectHashMap<Bucket> bucketsFoundInChain = new LongObjectHashMap<>();
            Dictionary<long, Bucket> bucketsFoundInChain = new Dictionary<long, Bucket>();
            buckets.validateInternalState();

            DirectOrder order = asksChain ? bestAskOrder : bestBidOrder;

            if (order != null && order.Next != null)
            {
                thrw("best order has not-null next reference");
            }

            //        log.Debug("----------- validating {} --------- ", asksChain ? OrderAction.ASK : OrderAction.BID);

            long lastPrice = -1;
            long expectedBucketVolume = 0;
            int expectedBucketOrders = 0;
            DirectOrder lastOrder = null;

            while (order != null)
            {

                if (ordersInChain.ContainsKey(order.OrderId))
                {
                    thrw("duplicate orderid in the chain");
                }
                ordersInChain[order.OrderId] = order;

                //log.Debug("id:{} p={} +{}", order.orderId, order.price, order.size - order.filled);
                expectedBucketVolume += order.Size - order.Filled;
                expectedBucketOrders++;

                if (lastOrder != null && order.Next != lastOrder)
                {
                    thrw("incorrect next reference");
                }
                if (order.Parent.tail.Price != order.Price)
                {
                    thrw("price of parent.tail differs");
                }
                if (lastPrice != -1 && order.Price != lastPrice)
                {
                    if (asksChain ^ order.Price > lastPrice)
                    {
                        thrw("unexpected price change direction");
                    }
                    if (order.Next.Parent == order.Parent)
                    {
                        thrw("unexpected price change within same bucket");
                    }
                }

                if (order.Parent.tail == order)
                {
                    if (order.Parent.volume != expectedBucketVolume)
                    {
                        thrw("bucket volume does not match orders chain sizes");
                    }
                    if (order.Parent.numOrders != expectedBucketOrders)
                    {
                        thrw("bucket numOrders does not match orders chain length");
                    }
                    if (order.Prev != null && order.Prev.Price == order.Price)
                    {
                        thrw("previous bucket has the same price");
                    }
                    expectedBucketVolume = 0;
                    expectedBucketOrders = 0;
                }

                if (!bucketsFoundInChain.TryGetValue(order.Price, out Bucket knownBucket))
                {
                    bucketsFoundInChain.Add(order.Price, order.Parent);
                }
                else if (knownBucket != order.Parent)
                {
                    thrw("found two different buckets having same price");
                }

                if (asksChain ^ order.Action == OrderAction.ASK)
                {
                    thrw("not expected order action");
                }

                lastPrice = order.Price;
                lastOrder = order;
                order = order.Prev;
            }

            // validate last order
            if (lastOrder != null && lastOrder.Parent.tail != lastOrder)
            {
                thrw("last order is not a tail");
            }

            //        log.Debug("-------- validateChain ----- asksChain={} ", asksChain);
            buckets.forEach((price, bucket) =>
            {
                //            log.Debug("Remove {} ", price);
                if (bucketsFoundInChain.Remove(price) != true) thrw("bucket in the price-tree not found in the chain");
            }, int.MaxValue);

            if (bucketsFoundInChain.Count != 0)
            {
                thrw("found buckets in the chain that not discoverable from the price-tree");
            }
        }

        //    private void dumpNearOrders(final DirectOrder order, int maxNeighbors) {
        //        if (order == null) {
        //            log.Debug("no orders");
        //            return;
        //        }
        //        DirectOrder p = order;
        //        for (int i = 0; i < maxNeighbors && p.prev != null; i++) {
        //            p = p.prev;
        //        }
        //        for (int i = 0; i < maxNeighbors * 2 && p != null; i++) {
        //            log.Debug(((p == order) ? "*" : " ") + "  {}\t -> \t{}", p, p.parent);
        //            p = p.next;
        //        }
        //    }

        private void thrw(string msg)
        {
            throw new NotImplementedException(msg);
            //throw new IllegalStateException(msg);
        }

        public OrderBookImplType getImplementationType()
        {
            return OrderBookImplType.DIRECT;
        }

        public List<Order> findUserOrders(long uid)
        {
            List<Order> list = new List<Order>();
            orderIdIndex.forEach((orderId, order) =>
            {
                if (order.Uid == uid)
                {
                    list.Add(Order.Builder()
                            .orderId(orderId)
                            .price(order.Price)
                            .size(order.Size)
                            .filled(order.Filled)
                            .reserveBidPrice(order.ReserveBidPrice)
                            .action(order.Action)
                            .uid(order.Uid)
                            .timestamp(order.Timestamp)
                            .build());
                }
            }, int.MaxValue);

            return list;
        }

        public CoreSymbolSpecification getSymbolSpec()
        {
            return symbolSpec;
        }

        //public Stream<DirectOrder> askOrdersStream(bool sortedIgnore)
        //{
        //    return StreamSupport.stream(new OrdersSpliterator(bestAskOrder), false);
        //}

        //public Stream<DirectOrder> bidOrdersStream(boolean sortedIgnore)
        //{
        //    return StreamSupport.stream(new OrdersSpliterator(bestBidOrder), false);
        //}

        public void fillAsks(int size, L2MarketData data)
        {
            data.AskSize = 0;
            askPriceBuckets.forEach((p, bucket) =>
            {
                int i = data.AskSize++;
                data.AskPrices[i] = bucket.tail.Price;
                data.AskVolumes[i] = bucket.volume;
                data.AskOrders[i] = bucket.numOrders;
            }, size);
        }

        public void fillBids(int size, L2MarketData data)
        {
            data.BidSize = 0;
            bidPriceBuckets.forEachDesc((p, bucket) =>
            {
                int i = data.BidSize++;
                data.BidPrices[i] = bucket.tail.Price;
                data.BidVolumes[i] = bucket.volume;
                data.BidOrders[i] = bucket.numOrders;
            }, size);
        }

        public int getTotalAskBuckets(int limit)
        {
            return askPriceBuckets.size(limit);
        }

        public int getTotalBidBuckets(int limit)
        {
            return bidPriceBuckets.size(limit);
        }

        public IEnumerable<IOrder> askOrdersStream(bool sorted)
        {
            var current = bestAskOrder;
            while (current != null)
            {
                yield return current;
                current = current.Prev;
            }
        }

        public IEnumerable<IOrder> bidOrdersStream(bool sorted)
        {
            var current = bestBidOrder;
            while (current != null)
            {
                yield return current;
                current = current.Prev;
            }
        }

        //public void writeMarshallable(BytesOut bytes)
        //{
        //    bytes.writeByte(getImplementationType().getCode());
        //    symbolSpec.writeMarshallable(bytes);
        //    bytes.writeInt(orderIdIndex.size(int.MaxValue));
        //    askOrdersStream(true).forEach(order->order.writeMarshallable(bytes));
        //    bidOrdersStream(true).forEach(order->order.writeMarshallable(bytes));
        //}
    }
}
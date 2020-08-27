using Exchange.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Orderbook
{
    public sealed class OrdersBucketNaive : IComparable<OrdersBucketNaive>//, WriteBytesMarshallable
    {
        public long Price { get; }
        public long TotalVolume { get; private set; }

        private readonly Dictionary<long, Order> entries;


        public OrdersBucketNaive(long price)
        {
            Price = price;
            this.entries = new Dictionary<long, Order>();
            TotalVolume = 0;
        }

        //public OrdersBucketNaive(BytesIn bytes)
        //{
        //    this.price = bytes.readLong();
        //    this.entries = SerializationUtils.readLongMap(bytes, LinkedHashMap::new, Order::new);
        //    this.totalVolume = bytes.readLong();
        //}

        /**
         * Put a new order into bucket
         *
         * @param order - order
         */
        public void put(Order order)
        {
            entries[order.OrderId] = order;
            TotalVolume += order.Size - order.Filled;
        }

        /**
         * Remove order from the bucket
         *
         * @param orderId - order id
         * @param uid     - order uid
         * @return order if removed, or null if not found
         */
        public Order remove(long orderId, long uid)
        {
            //        log.debug("removing order: {}", order);
            if (!entries.TryGetValue(orderId, out Order order) || order.Uid != uid)
            {
                return null;
            }

            entries.Remove(orderId);

            TotalVolume -= order.Size - order.Filled;
            return order;
        }

        /**
         * Collect a list of matching orders starting from eldest records
         * Completely matching orders will be removed, partially matched order kept in the bucked.
         *
         * @param volumeToCollect - volume to collect
         * @param activeOrder     - for getReserveBidPrice
         * @param helper          - events helper
         * @return - total matched volume, events, completed orders to remove
         */
        public MatcherResult match(long volumeToCollect, IOrder activeOrder, OrderBookEventsHelper helper)
        {

            //        log.debug("---- match: {}", volumeToCollect);

            IEnumerator<KeyValuePair<long, Order>> iterator = entries.GetEnumerator();

            long totalMatchingVolume = 0;

            List<long> ordersToRemove = new List<long>();

            MatcherTradeEvent eventsHead = null;
            MatcherTradeEvent eventsTail = null;

            var toRemove = new List<long>();

            // iterate through all orders
            while (iterator.MoveNext() && volumeToCollect > 0)
            {
                KeyValuePair<long, Order> next = iterator.Current;
                Order order = next.Value;

                // calculate exact volume can fill for this order
                //            log.debug("volumeToCollect={} order: s{} f{}", volumeToCollect, order.size, order.filled);
                long v = Math.Min(volumeToCollect, order.Size - order.Filled);
                totalMatchingVolume += v;
                //            log.debug("totalMatchingVolume={} v={}", totalMatchingVolume, v);

                order.Filled += v;
                volumeToCollect -= v;
                TotalVolume -= v;

                // remove from order book filled orders
                bool fullMatch = order.Size == order.Filled;

                long bidderHoldPrice = order.Action == OrderAction.ASK ? activeOrder.ReserveBidPrice : order.ReserveBidPrice;
                MatcherTradeEvent tradeEvent = helper.sendTradeEvent(order, fullMatch, volumeToCollect == 0, v, bidderHoldPrice);

                if (eventsTail == null)
                {
                    eventsHead = tradeEvent;
                }
                else
                {
                    eventsTail.NextEvent = tradeEvent;
                }
                eventsTail = tradeEvent;

                if (fullMatch)
                {
                    ordersToRemove.Add(order.OrderId);
                    //iterator.remove();
                    toRemove.Add(next.Key);
                }
            }

            toRemove.ForEach(x => entries.Remove(x));

            return new MatcherResult(eventsHead, eventsTail, totalMatchingVolume, ordersToRemove);
        }

        /**
         * Get number of orders in the bucket
         *
         * @return number of orders in the bucket
         */
        public int getNumOrders()
        {
            return entries.Count;
        }

        /**
         * Reduce size of the order
         *
         * @param reduceSize - size to reduce (difference)
         */
        public void reduceSize(long reduceSize)
        {

            TotalVolume -= reduceSize;
        }

        public void validate()
        {
            long sum = entries.Values.Select(c => c.Size - c.Filled).Sum();
            if (sum != TotalVolume)
            {
                string msg = string.Format("totalVolume=%d calculated=%d", TotalVolume, sum);
                throw new InvalidOperationException(msg);
            }
        }

        public Order findOrder(long orderId)
        {
            return entries[orderId];
        }

        /**
         * Inefficient method - for testing only
         *
         * @return new array with references to orders, preserving execution queue order
         */
        public List<Order> getAllOrders()
        {
            return new List<Order>(entries.Values);
        }


        /**
         * execute some action for each order (preserving execution queue order)
         *
         * @param consumer action consumer function
         */
        public void forEachOrder(Action<Order> consumer)
        {
            foreach (var value in entries.Values)
                consumer(value);
        }

        public String dumpToSingleLine()
        {
            String orders = string.Join(", ", getAllOrders().Select(o => String.Format("id%d_L%d_F%d", o.OrderId, o.Size, o.Filled)));

            return String.Format("%d : vol:%d num:%d : %s", Price, TotalVolume, getNumOrders(), orders);
        }

        //public void writeMarshallable(BytesOut bytes)
        //    {
        //        bytes.writeLong(price);
        //        SerializationUtils.marshallLongMap(entries, bytes);
        //        bytes.writeLong(totalVolume);
        //    }

        public int CompareTo(OrdersBucketNaive other)
        {
            return Price.CompareTo(other.Price);
        }

        public int hashCode()
        {
            return 97 * Price.GetHashCode() +
                   997 * entries.Values.ToArray().GetHashCode();

            //return Objects.hash(
            //        price,
            //        Arrays.hashCode(entries.values().toArray(new Order[0])));
        }

        public bool equals(Object o)
        {
            if (o == this) return true;
            if (o == null) return false;
            if (!(o is OrdersBucketNaive)) return false;
            OrdersBucketNaive other = (OrdersBucketNaive)o;
            return Price == other.Price
                    && getAllOrders().Equals(other.getAllOrders());
        }

        public sealed class MatcherResult
        {
            public MatcherTradeEvent EventsChainHead { get; set; }
            public MatcherTradeEvent EventsChainTail { get; set; }
            public long Volume { get; set; }
            public List<long> OrdersToRemove { get; set; }

            public MatcherResult(MatcherTradeEvent eventsChainHead, MatcherTradeEvent eventsChainTail, long volume, List<long> ordersToRemove)
            {
                EventsChainHead = eventsChainHead;
                EventsChainTail = eventsChainTail;
                Volume = volume;
                OrdersToRemove = ordersToRemove;
            }
        }

    }

}

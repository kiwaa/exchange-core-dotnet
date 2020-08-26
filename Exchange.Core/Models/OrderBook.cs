using System.Collections.Generic;

namespace Exchange.Core
{
    public class OrderBook
    {
        public int Symbol { get; set; }
        public List<OrderBookRecord> Asks { get; set; }
        public List<OrderBookRecord> Bids { get; set; }
        public long Timestamp { get; set; }

        public OrderBook(int symbol, List<OrderBookRecord> asks, List<OrderBookRecord> bids, long timestamp)
        {
            Symbol = symbol;
            Asks = asks;
            Bids = bids;
            Timestamp = timestamp;
        }
    }
}

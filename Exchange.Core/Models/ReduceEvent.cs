namespace Exchange.Core
{
    public class ReduceEvent
    {
        public int Symbol { get; set; }
        public long ReducedVolume { get; set; }
        public bool OrderCompleted { get; set; }
        public long Price { get; set; }
        public long OrderId { get; set; }
        public long Uid { get; set; }
        public long Timestamp { get; set; }

        public ReduceEvent(int symbol, long reducedVolume, bool orderCompleted, long price, long orderId, long uid, long timestamp)
        {
            Symbol = symbol;
            ReducedVolume = reducedVolume;
            OrderCompleted = orderCompleted;
            Price = price;
            OrderId = orderId;
            Uid = uid;
            Timestamp = timestamp;
        }
    }
}
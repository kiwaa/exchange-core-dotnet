namespace Exchange.Core
{
    public class RejectEvent
    {
        public int Symbol { get; set; }
        public long RejectedVolume { get; set; }
        public long Price { get; set; }
        public long OrderId { get; set; }
        public long Uid { get; set; }
        public long Timestamp { get; set; }

        public RejectEvent(int symbol, long rejectedVolume, long price, long orderId, long uid, long timestamp)
        {
            Symbol = symbol;
            RejectedVolume = rejectedVolume;
            Price = price;
            OrderId = orderId;
            Uid = uid;
            Timestamp = timestamp;
        }
    }
}
using Exchange.Core.Common;
using System.Collections.Generic;

namespace Exchange.Core
{
    public class TradeEvent
    {
        public int Symbol { get; set; }
        public long TotalVolume { get; set; }
        public long TakerOrderId { get; set; }
        public long TakerUid { get; set; }
        public OrderAction TakerAction { get; set; }
        public bool TakeOrderCompleted { get; set; }
        public long Timestamp { get; set; }
        public List<Trade> Trades { get; set; }

        public TradeEvent(int symbol, long totalVolume, long takerOrderId, long takerUid, OrderAction takerAction, bool takeOrderCompleted, long timestamp, List<Trade> trades)
        {
            Symbol = symbol;
            TotalVolume = totalVolume;
            TakerOrderId = takerOrderId;
            TakerUid = takerUid;
            TakerAction = takerAction;
            TakeOrderCompleted = takeOrderCompleted;
            Timestamp = timestamp;
            Trades = trades;
        }
    }
}
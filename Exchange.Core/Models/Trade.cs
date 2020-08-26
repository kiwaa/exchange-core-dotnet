using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core
{
    public class Trade
    {
        public long MakerOrderId { get; set; }
        public long MakerUid { get; set; }
        public bool MakerOrderCompleted { get; set; }
        public long Price { get; set; }
        public long Volume { get; set; }

        public Trade(long makerOrderId, long makerUid, bool makerOrderCompleted, long price, long volume)
        {
            MakerOrderId = makerOrderId;
            MakerUid = makerUid;
            MakerOrderCompleted = makerOrderCompleted;
            Price = price;
            Volume = volume;
        }
    }
}

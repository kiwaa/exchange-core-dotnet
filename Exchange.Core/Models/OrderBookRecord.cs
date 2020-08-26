using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core
{
    public class OrderBookRecord
    {
        public long Price { get; set; }
        public long Volume { get; set; }
        public int Orders { get; set; }

        public OrderBookRecord(long price, long volume, int orders)
        {
            Price = price;
            Volume = volume;
            Orders = orders;
        }
    }
}

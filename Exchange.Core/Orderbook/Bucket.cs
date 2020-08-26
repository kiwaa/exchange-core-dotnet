using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Orderbook
{
    public sealed class Bucket
    {
        public long volume { get; set; }
        public int numOrders { get; set; }
        public DirectOrder tail { get; set; }
    }
}

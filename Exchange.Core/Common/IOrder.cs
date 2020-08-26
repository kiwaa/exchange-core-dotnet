using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common
{
    public interface IOrder //: StateHash
    {
        long Price { get; }
        long Size { get; }
        long Filled { get; }
        long Uid { get; }
        OrderAction Action { get; }
        long OrderId { get; }
        long Timestamp { get; }
        long ReserveBidPrice { get; }

    }

}

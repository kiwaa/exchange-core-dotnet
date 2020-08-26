using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common
{
    public enum MatcherEventType
    {

        // Trade event
        // Can be triggered by place ORDER or for MOVE order command.
        TRADE,

        // Reject event
        // Can happen only when MARKET order has to be rejected by Matcher Engine due lack of liquidity
        // That basically means no ASK (or BID) orders left in the order book for any price.
        // Before being rejected active order can be partially filled.
        REJECT,

        // After cancel/reduce order - risk engine should unlock deposit accordingly
        REDUCE,

        // Custom binary data attached
        BINARY_EVENT
    }
}

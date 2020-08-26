using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common.Cmd
{
    public enum OrderCommandType
    {
        PLACE_ORDER = 1,
        CANCEL_ORDER = 2,
        MOVE_ORDER = 3,
        REDUCE_ORDER = 4,

        ORDER_BOOK_REQUEST = 6,

        ADD_USER = 10,
        BALANCE_ADJUSTMENT = 11,
        SUSPEND_USER = 12,
        RESUME_USER = 13,

        BINARY_DATA_QUERY = 90,
        BINARY_DATA_COMMAND = 91,

        PERSIST_STATE_MATCHING = 110,
        PERSIST_STATE_RISK = 111,

        GROUPING_CONTROL = 118,
        NOP = 120,
        RESET = 124,
        SHUTDOWN_SIGNAL = 127,

        RESERVED_COMPRESSED = -1
    }
}

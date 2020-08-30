using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common
{
    public enum PositionDirection
    {
        LONG = 1,
        SHORT = -1,
        EMPTY = 0
    }

    public static class PositionDirectionHelper
    {
        public static PositionDirection of(OrderAction action)
        {
            return action == OrderAction.BID ? PositionDirection.LONG : PositionDirection.SHORT;
        }

    }
}

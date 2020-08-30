using Disruptor;
using Exchange.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common
{
    public enum CoreWaitStrategy
    {
        BUSY_SPIN,
        YIELDING,
        BLOCKING,
        // special case
        SECOND_STEP_NO_WAIT
    }

    public static class CoreWaitStrategyHelper
    {
        public static IWaitStrategy GetDisruptorWaitStrategy(CoreWaitStrategy strategy)
        {
            switch (strategy)
            {
                case CoreWaitStrategy.BUSY_SPIN:
                    return new BusySpinWaitStrategy();
                case CoreWaitStrategy.YIELDING:
                    return new YieldingWaitStrategy();
                case CoreWaitStrategy.BLOCKING:
                    return new BlockingSpinWaitWaitStrategy();
                case CoreWaitStrategy.SECOND_STEP_NO_WAIT:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }

        }

        public static bool IsBlock(CoreWaitStrategy strategy)
        {
            switch (strategy)
            {
                case CoreWaitStrategy.BUSY_SPIN:
                case CoreWaitStrategy.YIELDING:
                    return false;
                case CoreWaitStrategy.BLOCKING:
                    return true;
                case CoreWaitStrategy.SECOND_STEP_NO_WAIT:
                    return false;
                default:
                    throw new NotImplementedException();
            }
        }
        public static bool IsYield(CoreWaitStrategy strategy)
        {
            switch (strategy)
            {
                case CoreWaitStrategy.BUSY_SPIN:
                    return false;
                case CoreWaitStrategy.YIELDING:
                    return true;
                case CoreWaitStrategy.BLOCKING:
                case CoreWaitStrategy.SECOND_STEP_NO_WAIT:
                default:
                    return false;
            }
        }
    }
}

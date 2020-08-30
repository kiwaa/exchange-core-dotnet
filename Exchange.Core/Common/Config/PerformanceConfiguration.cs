using Exchange.Core.Orderbook;
using System;
using System.Threading.Tasks;

namespace Exchange.Core.Common.Config
{
    public sealed partial class PerformanceConfiguration
    {
        public static readonly PerformanceConfiguration DEFAULT = baseBuilder().build();


        public override string ToString()
        {
            return "PerformanceConfiguration{" +
                    "ringBufferSize=" + RingBufferSize +
                    ", matchingEnginesNum=" + MatchingEnginesNum +
                    ", riskEnginesNum=" + RiskEnginesNum +
                    ", msgsInGroupLimit=" + MsgsInGroupLimit +
                    ", maxGroupDurationNs=" + MaxGroupDurationNs +
                    ", threadFactory=" + (TaskScheduler == null ? null : typeof(TaskScheduler).getSimpleName()) +
                    ", waitStrategy=" + WaitStrategy +
                    '}';
        }

        // TODO add expected number of users and symbols

        public static PerformanceConfigurationBuilder baseBuilder() => Builder()
                    .ringBufferSize(16 * 1024)
                    .matchingEnginesNum(1)
                    .riskEnginesNum(1)
                    .msgsInGroupLimit(256)
                    .maxGroupDurationNs(10_000)
                    .taskScheduler(Thread::new)
                    .waitStrategy(CoreWaitStrategy.BLOCKING)
                    .binaryCommandsLz4CompressorFactory(() => LZ4Factory.fastestInstance().highCompressor())
                    .orderBookFactory(() => new OrderBookNaiveImpl());
    }
}
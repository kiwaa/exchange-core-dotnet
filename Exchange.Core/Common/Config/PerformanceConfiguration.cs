using Exchange.Core.Orderbook;
using System;
using System.Threading.Tasks;

namespace Exchange.Core.Common.Config
{
    public sealed partial class PerformanceConfiguration
    {
        public static readonly PerformanceConfiguration DEFAULT = baseBuilder().build();

        public static PerformanceConfiguration.PerformanceConfigurationBuilder latencyPerformanceBuilder()
        {

            return Builder()
                    .ringBufferSize(2 * 1024)
                    .matchingEnginesNum(1)
                    .riskEnginesNum(1)
                    .msgsInGroupLimit(256)
                    .maxGroupDurationNs(10_000)
                    //                    .taskScheduler(new AffinityThreadFactory(AffinityThreadFactory.ThreadAffinityMode.THREAD_AFFINITY_ENABLE_PER_LOGICAL_CORE))
                    .taskScheduler(TaskScheduler.Default)
                    .waitStrategy(CoreWaitStrategy.BUSY_SPIN)
                    .binaryCommandsLz4CompressorFactory(()=> new LZ4Compressor())
                    .orderBookFactory((spec, pool, helper, loggingCfg) => new OrderBookDirectImpl(spec, pool, helper, loggingCfg));
        }
        public override string ToString()
        {
            return "PerformanceConfiguration{" +
                    "ringBufferSize=" + RingBufferSize +
                    ", matchingEnginesNum=" + MatchingEnginesNum +
                    ", riskEnginesNum=" + RiskEnginesNum +
                    ", msgsInGroupLimit=" + MsgsInGroupLimit +
                    ", maxGroupDurationNs=" + MaxGroupDurationNs +
                    ", threadFactory=" + (TaskScheduler == null ? null : TaskScheduler.GetType().Name) +
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
                    .taskScheduler(TaskScheduler.Default)
                    .waitStrategy(CoreWaitStrategy.BLOCKING)
                    .binaryCommandsLz4CompressorFactory(() => new LZ4Compressor())
                    .orderBookFactory((spec, pool, helper, loggingCfg) => new OrderBookNaiveImpl(spec, pool, helper, loggingCfg));
    }
}
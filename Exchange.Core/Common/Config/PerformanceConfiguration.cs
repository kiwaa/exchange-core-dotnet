using System;

namespace Exchange.Core.Common.Config
{
    public class PerformanceConfiguration
    {
        public static readonly PerformanceConfiguration DEFAULT = baseBuilder().build();


        //public override string ToString()
        //{
        //    return "PerformanceConfiguration{" +
        //            "ringBufferSize=" + ringBufferSize +
        //            ", matchingEnginesNum=" + matchingEnginesNum +
        //            ", riskEnginesNum=" + riskEnginesNum +
        //            ", msgsInGroupLimit=" + msgsInGroupLimit +
        //            ", maxGroupDurationNs=" + maxGroupDurationNs +
        //            ", threadFactory=" + (threadFactory == null ? null : threadFactory.getClass().getSimpleName()) +
        //            ", waitStrategy=" + waitStrategy +
        //            ", orderBookFactory=" + (orderBookFactory == null ? null : orderBookFactory.getClass().getSimpleName()) +
        //            ", binaryCommandsLz4CompressorFactory=" + (binaryCommandsLz4CompressorFactory == null ? null : binaryCommandsLz4CompressorFactory.getClass().getSimpleName()) +
        //            '}';
        //}

        // TODO add expected number of users and symbols

        public static PerformanceConfigurationBuilder baseBuilder() => builder()
                    .ringBufferSize(16 * 1024)
                    .matchingEnginesNum(1)
                    .riskEnginesNum(1)
                    .msgsInGroupLimit(256)
                    .maxGroupDurationNs(10_000);
                    //.threadFactory(Thread::new)
                    //.waitStrategy(CoreWaitStrategy.BLOCKING)
                    //.binaryCommandsLz4CompressorFactory(() => LZ4Factory.fastestInstance().highCompressor())
                    //.orderBookFactory(OrderBookNaiveImpl::new);

        private static PerformanceConfigurationBuilder builder()
        {
            throw new NotImplementedException();
        }

        public class PerformanceConfigurationBuilder
        {
            internal PerformanceConfigurationBuilder matchingEnginesNum(int v)
            {
                throw new NotImplementedException();
            }

            internal PerformanceConfigurationBuilder maxGroupDurationNs(int v)
            {
                throw new NotImplementedException();
            }

            internal PerformanceConfigurationBuilder msgsInGroupLimit(int v)
            {
                throw new NotImplementedException();
            }

            internal PerformanceConfigurationBuilder ringBufferSize(int v)
            {
                throw new NotImplementedException();
            }

            internal PerformanceConfigurationBuilder riskEnginesNum(int v)
            {
                throw new NotImplementedException();
            }

            internal PerformanceConfigurationBuilder threadFactory(object p1, object p2)
            {
                throw new NotImplementedException();
            }

            internal PerformanceConfigurationBuilder waitStrategy(object bLOCKING)
            {
                throw new NotImplementedException();
            }

            internal PerformanceConfigurationBuilder binaryCommandsLz4CompressorFactory(object bLOCKING)
            {
                throw new NotImplementedException();
            }
            internal PerformanceConfigurationBuilder orderBookFactory(object bLOCKING)
            {
                throw new NotImplementedException();
            }

            internal PerformanceConfiguration build()
            {
                throw new NotImplementedException();
            }
        }
    }
}
using Exchange.Core.Common.Config;
using Exchange.Core.Tests.Tests.Utils;
using Exchange.Core.Tests.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Tests.Perf
{
    [TestFixture]
    public sealed class PerfHiccups
    {


        [Test]
    public void testHiccupMargin()
        {
            LatencyTestsModule.hiccupTestImpl(
                    PerformanceConfiguration.latencyPerformanceBuilder()
                            .ringBufferSize(2 * 1024)
                            .matchingEnginesNum(1)
                            .riskEnginesNum(1)
                            .msgsInGroupLimit(256)
                            .build(),
                    TestDataParameters.singlePairMarginBuilder().build(),
                    InitialStateConfiguration.CLEAN_TEST,
                    3);
        }


    }
}

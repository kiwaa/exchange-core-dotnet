using Exchange.Core.Common.Config;
using Exchange.Core.Tests.Integration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Tests.Integration
{
    [TestFixture]
    public sealed class ITExchangeCoreIntegrationRejectionLatency : ITExchangeCoreIntegrationRejection
    {

        public override PerformanceConfiguration getPerformanceConfiguration()
        {
            return PerformanceConfiguration.latencyPerformanceBuilder().build();
        }
    }
}

using Exchange.Core.Processors;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common.Api.Reports
{
    public sealed class StateHashReportQuery : IReportQuery<StateHashReportResult>
    {

        public StateHashReportQuery(IBytesIn bytesIn)
        {
            // do nothing
        }

        public int getReportTypeCode()
        {
            return (int)ReportType.STATE_HASH;
        }

        public StateHashReportResult createResult(IEnumerable<IBytesIn> sections)
        {
            return StateHashReportResult.merge(sections);
        }

        public StateHashReportResult process(MatchingEngineRouter matchingEngine)
        {

            throw new NotImplementedException();
        }

        public StateHashReportResult process(RiskEngine riskEngine)
        {
            throw new NotImplementedException();
        }

        public void writeMarshallable(IBytesOut bytes)
        {
            // do nothing
        }
    }
}

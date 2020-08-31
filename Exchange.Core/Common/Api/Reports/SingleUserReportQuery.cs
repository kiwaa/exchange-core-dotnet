using Exchange.Core.Processors;
using OpenHFT.Chronicle.WireMock;
using System.Collections.Generic;

namespace Exchange.Core.Common.Api.Reports
{
    internal class SingleUserReportQuery : IReportQuery<SingleUserReportResult>
    {
        private int v;

        public SingleUserReportQuery(int v)
        {
            this.v = v;
        }

        public SingleUserReportResult createResult(IEnumerable<IBytesIn> sections)
        {
            throw new System.NotImplementedException();
        }

        public int getReportTypeCode()
        {
            throw new System.NotImplementedException();
        }

        public SingleUserReportResult process(MatchingEngineRouter matchingEngine)
        {
            throw new System.NotImplementedException();
        }

        public SingleUserReportResult process(RiskEngine riskEngine)
        {
            throw new System.NotImplementedException();
        }

        public void writeMarshallable(IBytesOut bytes)
        {
            throw new System.NotImplementedException();
        }
    }
}
using Exchange.Core.Processors;
using OpenHFT.Chronicle.WireMock;
using System.Collections.Generic;

namespace Exchange.Core.Common.Api.Reports
{
    internal class TotalCurrencyBalanceReportQuery : IReportQuery<TotalCurrencyBalanceReportResult>
    {
        public TotalCurrencyBalanceReportQuery()
        {
        }

        public TotalCurrencyBalanceReportResult createResult(IEnumerable<IBytesIn> sections)
        {
            throw new System.NotImplementedException();
        }

        public int getReportTypeCode()
        {
            throw new System.NotImplementedException();
        }

        public TotalCurrencyBalanceReportResult process(MatchingEngineRouter matchingEngine)
        {
            throw new System.NotImplementedException();
        }

        public TotalCurrencyBalanceReportResult process(RiskEngine riskEngine)
        {
            throw new System.NotImplementedException();
        }
    }
}
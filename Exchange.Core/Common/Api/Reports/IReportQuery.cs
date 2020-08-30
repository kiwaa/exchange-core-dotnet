using Exchange.Core.Processors;
using OpenHFT.Chronicle.WireMock;
using System.Collections.Generic;

namespace Exchange.Core.Common.Api.Reports
{
    public interface IReportQuery<T> : IWriteBytesMarshallable where T : IReportResult
    {

    /**
     * @return report type code (integer)
     */
    int getReportTypeCode();

        /**
         * @return report map-reduce constructor
         */
        T createResult(IEnumerable<IBytesIn> sections);

        /**
         * Report main logic.
         * This method is executed by matcher engine thread.
         *
         * @param matchingEngine matcher engine instance
         * @return custom result
         */
        T process(MatchingEngineRouter matchingEngine);

        /**
         * Report main logic
         * This method is executed by risk engine thread.
         *
         * @param riskEngine risk engine instance.
         * @return custom result
         */
        T process(RiskEngine riskEngine);
    }

}
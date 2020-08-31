using OpenHFT.Chronicle.WireMock;
using System;

namespace Exchange.Core.Common.Api.Reports
{
    internal class SingleUserReportResult : IReportResult
    {
        public void writeMarshallable(IBytesOut bytes)
        {
            throw new NotImplementedException();
        }

        internal string getAccounts()
        {
            throw new NotImplementedException();
        }
    }
}
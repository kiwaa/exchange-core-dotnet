using Exchange.Core.Utils;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Exchange.Core.Common.Api.Reports
{
    public sealed partial class TotalCurrencyBalanceReportResult : IReportResult
    {
        public void writeMarshallable(IBytesOut bytes)
        {
            throw new NotImplementedException();
        }

        public bool isGlobalBalancesAllZero()
        {
            return getGlobalBalancesSum().All(amount => amount.Value == 0L);
        }

        public Dictionary<int, long> getGlobalBalancesSum()
        {
            return SerializationUtils.mergeSum(AccountBalances, OrdersBalances, Fees, Adjustments, Suspends);
        }

    }
}
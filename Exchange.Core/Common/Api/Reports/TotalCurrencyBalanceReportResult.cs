using Exchange.Core.Utils;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Exchange.Core.Common.Api.Reports
{
    public sealed partial class TotalCurrencyBalanceReportResult : IReportResult
    {
        private TotalCurrencyBalanceReportResult(IBytesIn bytesIn)
        {
            this.AccountBalances = SerializationUtils.readNullable(bytesIn, SerializationUtils.readIntLongHashMap);
            this.Fees = SerializationUtils.readNullable(bytesIn, SerializationUtils.readIntLongHashMap);
            this.Adjustments = SerializationUtils.readNullable(bytesIn, SerializationUtils.readIntLongHashMap);
            this.Suspends = SerializationUtils.readNullable(bytesIn, SerializationUtils.readIntLongHashMap);
            this.OrdersBalances = SerializationUtils.readNullable(bytesIn, SerializationUtils.readIntLongHashMap);
            this.OpenInterestLong = SerializationUtils.readNullable(bytesIn, SerializationUtils.readIntLongHashMap);
            this.OpenInterestShort = SerializationUtils.readNullable(bytesIn, SerializationUtils.readIntLongHashMap);
        }

        public void writeMarshallable(IBytesOut bytes)
        {
            SerializationUtils.marshallNullable(AccountBalances, bytes, SerializationUtils.marshallIntLongHashMap);
            SerializationUtils.marshallNullable(Fees, bytes, SerializationUtils.marshallIntLongHashMap);
            SerializationUtils.marshallNullable(Adjustments, bytes, SerializationUtils.marshallIntLongHashMap);
            SerializationUtils.marshallNullable(Suspends, bytes, SerializationUtils.marshallIntLongHashMap);
            SerializationUtils.marshallNullable(OrdersBalances, bytes, SerializationUtils.marshallIntLongHashMap);
            SerializationUtils.marshallNullable(OpenInterestLong, bytes, SerializationUtils.marshallIntLongHashMap);
            SerializationUtils.marshallNullable(OpenInterestShort, bytes, SerializationUtils.marshallIntLongHashMap);
        }

        public Dictionary<int,long> getGlobalBalancesSum()
        {
            return SerializationUtils.mergeSum(AccountBalances, OrdersBalances, Fees, Adjustments, Suspends);
        }
        public Dictionary<int, long> getClientsBalancesSum()
        {
            return SerializationUtils.mergeSum(AccountBalances, OrdersBalances, Suspends);
        }

        public bool isGlobalBalancesAllZero()
        {
            return getGlobalBalancesSum().All(amount => amount.Value == 0L);
        }

        public static TotalCurrencyBalanceReportResult createEmpty()
        {
            return new TotalCurrencyBalanceReportResult(
                    null, null, null, null, null, null, null);
        }

        public static TotalCurrencyBalanceReportResult ofOrderBalances(Dictionary<int,long> currencyBalance)
        {
            return new TotalCurrencyBalanceReportResult(
                    null, null, null, null, currencyBalance, null, null);
        }

        public static TotalCurrencyBalanceReportResult merge(IEnumerable<IBytesIn> pieces)
        {
            return pieces
                    .Select(x => new TotalCurrencyBalanceReportResult(x))
                    .Aggregate(
                            TotalCurrencyBalanceReportResult.createEmpty(),
                            (a, b)=> new TotalCurrencyBalanceReportResult(
                                    SerializationUtils.mergeSum(a.AccountBalances, b.AccountBalances),
                                    SerializationUtils.mergeSum(a.Fees, b.Fees),
                                    SerializationUtils.mergeSum(a.Adjustments, b.Adjustments),
                                    SerializationUtils.mergeSum(a.Suspends, b.Suspends),
                                    SerializationUtils.mergeSum(a.OrdersBalances, b.OrdersBalances),
                                    SerializationUtils.mergeSum(a.OpenInterestLong, b.OpenInterestLong),
                                    SerializationUtils.mergeSum(a.OpenInterestShort, b.OpenInterestShort)));
        }
    }
}
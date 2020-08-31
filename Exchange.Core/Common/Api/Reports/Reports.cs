using System;
using System.Collections.Generic;

namespace Exchange.Core.Common.Api.Reports
{
    public sealed partial class TotalCurrencyBalanceReportResult : ApiCommand, IEquatable<TotalCurrencyBalanceReportResult>
    {
        public Dictionary<int,long> AccountBalances { get; }
        public Dictionary<int,long> Fees { get; }
        public Dictionary<int,long> Adjustments { get; }
        public Dictionary<int,long> Suspends { get; }
        public Dictionary<int,long> OrdersBalances { get; }
        public Dictionary<int,long> OpenInterestLong { get; }
        public Dictionary<int,long> OpenInterestShort { get; }
        public TotalCurrencyBalanceReportResult(Dictionary<int,long> accountBalances, Dictionary<int,long> fees, Dictionary<int,long> adjustments, Dictionary<int,long> suspends, Dictionary<int,long> ordersBalances, Dictionary<int,long> openInterestLong, Dictionary<int,long> openInterestShort)
        {
            AccountBalances = accountBalances;
            Fees = fees;
            Adjustments = adjustments;
            Suspends = suspends;
            OrdersBalances = ordersBalances;
            OpenInterestLong = openInterestLong;
            OpenInterestShort = openInterestShort;
        }

        public bool Equals(TotalCurrencyBalanceReportResult other)
        {
              return AccountBalances.Equals(other.AccountBalances) && Fees.Equals(other.Fees) && Adjustments.Equals(other.Adjustments) && Suspends.Equals(other.Suspends) && OrdersBalances.Equals(other.OrdersBalances) && OpenInterestLong.Equals(other.OpenInterestLong) && OpenInterestShort.Equals(other.OpenInterestShort);
        }

        public static TotalCurrencyBalanceReportResultBuilder Builder()
        {
              return new TotalCurrencyBalanceReportResultBuilder();
        }

        public sealed class TotalCurrencyBalanceReportResultBuilder
        {
            private Dictionary<int,long> _accountBalances;
            private Dictionary<int,long> _fees;
            private Dictionary<int,long> _adjustments;
            private Dictionary<int,long> _suspends;
            private Dictionary<int,long> _ordersBalances;
            private Dictionary<int,long> _openInterestLong;
            private Dictionary<int,long> _openInterestShort;

            public TotalCurrencyBalanceReportResultBuilder accountBalances(Dictionary<int,long> value)
            {
                _accountBalances = value;
                return this;
            }
            public TotalCurrencyBalanceReportResultBuilder fees(Dictionary<int,long> value)
            {
                _fees = value;
                return this;
            }
            public TotalCurrencyBalanceReportResultBuilder adjustments(Dictionary<int,long> value)
            {
                _adjustments = value;
                return this;
            }
            public TotalCurrencyBalanceReportResultBuilder suspends(Dictionary<int,long> value)
            {
                _suspends = value;
                return this;
            }
            public TotalCurrencyBalanceReportResultBuilder ordersBalances(Dictionary<int,long> value)
            {
                _ordersBalances = value;
                return this;
            }
            public TotalCurrencyBalanceReportResultBuilder openInterestLong(Dictionary<int,long> value)
            {
                _openInterestLong = value;
                return this;
            }
            public TotalCurrencyBalanceReportResultBuilder openInterestShort(Dictionary<int,long> value)
            {
                _openInterestShort = value;
                return this;
            }

            public TotalCurrencyBalanceReportResult build()
            {
                return new TotalCurrencyBalanceReportResult(_accountBalances, _fees, _adjustments, _suspends, _ordersBalances, _openInterestLong, _openInterestShort);
            }
        }
    }
}


				

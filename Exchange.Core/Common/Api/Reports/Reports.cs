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
    public sealed partial class SingleUserReportResult : ApiCommand, IEquatable<SingleUserReportResult>
    {
        public long Uid { get; }
        public UserStatus UserStatus { get; }
        public Dictionary<int,long> Accounts { get; }
        public Dictionary<int,Position> Positions { get; }
        public Dictionary<int,List<Order>> Orders { get; }
        public QueryExecutionStatus QueryExecutionStatus { get; }
        public SingleUserReportResult(long uid, UserStatus userStatus, Dictionary<int,long> accounts, Dictionary<int,Position> positions, Dictionary<int,List<Order>> orders, QueryExecutionStatus queryExecutionStatus)
        {
            Uid = uid;
            UserStatus = userStatus;
            Accounts = accounts;
            Positions = positions;
            Orders = orders;
            QueryExecutionStatus = queryExecutionStatus;
        }

        public bool Equals(SingleUserReportResult other)
        {
              return Uid.Equals(other.Uid) && UserStatus.Equals(other.UserStatus) && Accounts.Equals(other.Accounts) && Positions.Equals(other.Positions) && Orders.Equals(other.Orders) && QueryExecutionStatus.Equals(other.QueryExecutionStatus);
        }

        public static SingleUserReportResultBuilder Builder()
        {
              return new SingleUserReportResultBuilder();
        }

        public sealed class SingleUserReportResultBuilder
        {
            private long _uid;
            private UserStatus _userStatus;
            private Dictionary<int,long> _accounts;
            private Dictionary<int,Position> _positions;
            private Dictionary<int,List<Order>> _orders;
            private QueryExecutionStatus _queryExecutionStatus;

            public SingleUserReportResultBuilder uid(long value)
            {
                _uid = value;
                return this;
            }
            public SingleUserReportResultBuilder userStatus(UserStatus value)
            {
                _userStatus = value;
                return this;
            }
            public SingleUserReportResultBuilder accounts(Dictionary<int,long> value)
            {
                _accounts = value;
                return this;
            }
            public SingleUserReportResultBuilder positions(Dictionary<int,Position> value)
            {
                _positions = value;
                return this;
            }
            public SingleUserReportResultBuilder orders(Dictionary<int,List<Order>> value)
            {
                _orders = value;
                return this;
            }
            public SingleUserReportResultBuilder queryExecutionStatus(QueryExecutionStatus value)
            {
                _queryExecutionStatus = value;
                return this;
            }

            public SingleUserReportResult build()
            {
                return new SingleUserReportResult(_uid, _userStatus, _accounts, _positions, _orders, _queryExecutionStatus);
            }
        }
    }
}


				

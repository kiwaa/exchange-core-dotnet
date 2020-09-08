using Exchange.Core.Utils;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Exchange.Core.Common.Api.Reports
{
    public sealed partial class SingleUserReportResult : IReportResult
    {
        public static SingleUserReportResult IDENTITY = new SingleUserReportResult(0L, default, null, null, null, QueryExecutionStatus.OK);

        public static SingleUserReportResult createFromMatchingEngine(long uid, Dictionary<int, List<Order>> orders)
        {
            return new SingleUserReportResult(uid, default, null, null, orders, QueryExecutionStatus.OK);
        }

        public static SingleUserReportResult createFromRiskEngineFound(long uid, UserStatus userStatus, Dictionary<int,long> accounts, Dictionary<int, Position> positions)
        {
            return new SingleUserReportResult(uid, userStatus, accounts, positions, null, QueryExecutionStatus.OK);
        }

        public static SingleUserReportResult createFromRiskEngineNotFound(long uid)
        {
            return new SingleUserReportResult(uid, default, null, null, null, QueryExecutionStatus.USER_NOT_FOUND);
        }

        private SingleUserReportResult(IBytesIn bytesIn)
        {
            this.Uid = bytesIn.readLong();
            //        this.userProfile = bytesIn.readBoolean() ? new UserProfile(bytesIn) : null;
            this.UserStatus = bytesIn.readBool() ? (UserStatus)bytesIn.readByte() : default;
            this.Accounts = bytesIn.readBool() ? SerializationUtils.readIntLongHashMap(bytesIn) : null;
            this.Positions = bytesIn.readBool() ? SerializationUtils.readIntHashMap(bytesIn, x => new Position(x)) : null;
            this.Orders = bytesIn.readBool() ? SerializationUtils.readIntHashMap(bytesIn, b=>SerializationUtils.readList(b, x => new Order(x))) : null;
            this.QueryExecutionStatus = (QueryExecutionStatus)bytesIn.readInt();
        }

        public Dictionary<long, Order> fetchIndexedOrders()
        {
            return Orders.SelectMany(x => x.Value)
                    .ToDictionary(x => x.OrderId);
        }

        public void writeMarshallable(IBytesOut bytes)
        {

            bytes.writeLong(Uid);

            //        bytes.writeBoolean(userProfile != null);
            //        if (userProfile != null) {
            //            userProfile.writeMarshallable(bytes);
            //        }

            bytes.writeBool(UserStatus != null);
            if (UserStatus != null)
            {
                bytes.writeByte((byte)UserStatus);
            }

            bytes.writeBool(Accounts != null);
            if (Accounts != null)
            {
                SerializationUtils.marshallIntLongHashMap(Accounts, bytes);
            }

            bytes.writeBool(Positions != null);
            if (Positions != null)
            {
                SerializationUtils.marshallIntHashMap(Positions, bytes);
            }

            bytes.writeBool(Orders != null);
            if (Orders != null)
            {
                SerializationUtils.marshallIntHashMap(Orders, bytes, symbolOrders=>SerializationUtils.marshallList(symbolOrders, bytes));
            }
            bytes.writeInt((int)QueryExecutionStatus);

        }

        public static SingleUserReportResult merge(IEnumerable<IBytesIn> pieces)
        {
            return pieces
                    .Select(x => new SingleUserReportResult(x))
                    .Aggregate(
                            IDENTITY,
                            (a, b)=> new SingleUserReportResult(
                                    a.Uid,
                                    //                                SerializationUtils.preferNotNull(a.userProfile, b.userProfile),
                                    SerializationUtils.preferNotNull(a.UserStatus, b.UserStatus),
                                    SerializationUtils.preferNotNull(a.Accounts, b.Accounts),
                                    SerializationUtils.preferNotNull(a.Positions, b.Positions),
                                    SerializationUtils.mergeOverride(a.Orders, b.Orders),
                                    a.QueryExecutionStatus != QueryExecutionStatus.OK ? a.QueryExecutionStatus : b.QueryExecutionStatus));
        }
    }
}
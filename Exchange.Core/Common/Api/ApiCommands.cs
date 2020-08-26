using System;
using Exchange.Core.Common.Api.Binary;

namespace Exchange.Core.Common.Api
{
    public sealed partial class ApiPlaceOrder : ApiCommand, IEquatable<ApiPlaceOrder>
    {
        public long Price { get; }
        public long Size { get; }
        public long OrderId { get; }
        public OrderAction Action { get; }
        public OrderType OrderType { get; }
        public long Uid { get; }
        public int Symbol { get; }
        public int UserCookie { get; }
        public long ReservePrice { get; }
        public ApiPlaceOrder(long price, long size, long orderId, OrderAction action, OrderType orderType, long uid, int symbol, int userCookie, long reservePrice)
        {
            Price = price;
            Size = size;
            OrderId = orderId;
            Action = action;
            OrderType = orderType;
            Uid = uid;
            Symbol = symbol;
            UserCookie = userCookie;
            ReservePrice = reservePrice;
        }

        public bool Equals(ApiPlaceOrder other)
        {
              return Price.Equals(other.Price) && Size.Equals(other.Size) && OrderId.Equals(other.OrderId) && Action.Equals(other.Action) && OrderType.Equals(other.OrderType) && Uid.Equals(other.Uid) && Symbol.Equals(other.Symbol) && UserCookie.Equals(other.UserCookie) && ReservePrice.Equals(other.ReservePrice);
        }

        public static ApiPlaceOrderBuilder Builder()
        {
              return new ApiPlaceOrderBuilder();
        }

        public sealed class ApiPlaceOrderBuilder
        {
            private long _price;
            private long _size;
            private long _orderId;
            private OrderAction _action;
            private OrderType _orderType;
            private long _uid;
            private int _symbol;
            private int _userCookie;
            private long _reservePrice;

            public ApiPlaceOrderBuilder price(long value)
            {
                _price = value;
                return this;
            }
            public ApiPlaceOrderBuilder size(long value)
            {
                _size = value;
                return this;
            }
            public ApiPlaceOrderBuilder orderId(long value)
            {
                _orderId = value;
                return this;
            }
            public ApiPlaceOrderBuilder action(OrderAction value)
            {
                _action = value;
                return this;
            }
            public ApiPlaceOrderBuilder orderType(OrderType value)
            {
                _orderType = value;
                return this;
            }
            public ApiPlaceOrderBuilder uid(long value)
            {
                _uid = value;
                return this;
            }
            public ApiPlaceOrderBuilder symbol(int value)
            {
                _symbol = value;
                return this;
            }
            public ApiPlaceOrderBuilder userCookie(int value)
            {
                _userCookie = value;
                return this;
            }
            public ApiPlaceOrderBuilder reservePrice(long value)
            {
                _reservePrice = value;
                return this;
            }

            public ApiPlaceOrder build()
            {
                return new ApiPlaceOrder(_price, _size, _orderId, _action, _orderType, _uid, _symbol, _userCookie, _reservePrice);
            }
        }
    }
    public sealed partial class ApiMoveOrder : ApiCommand, IEquatable<ApiMoveOrder>
    {
        public long OrderId { get; }
        public long NewPrice { get; }
        public long Uid { get; }
        public int Symbol { get; }
        public ApiMoveOrder(long orderId, long newPrice, long uid, int symbol)
        {
            OrderId = orderId;
            NewPrice = newPrice;
            Uid = uid;
            Symbol = symbol;
        }

        public bool Equals(ApiMoveOrder other)
        {
              return OrderId.Equals(other.OrderId) && NewPrice.Equals(other.NewPrice) && Uid.Equals(other.Uid) && Symbol.Equals(other.Symbol);
        }

        public static ApiMoveOrderBuilder Builder()
        {
              return new ApiMoveOrderBuilder();
        }

        public sealed class ApiMoveOrderBuilder
        {
            private long _orderId;
            private long _newPrice;
            private long _uid;
            private int _symbol;

            public ApiMoveOrderBuilder orderId(long value)
            {
                _orderId = value;
                return this;
            }
            public ApiMoveOrderBuilder newPrice(long value)
            {
                _newPrice = value;
                return this;
            }
            public ApiMoveOrderBuilder uid(long value)
            {
                _uid = value;
                return this;
            }
            public ApiMoveOrderBuilder symbol(int value)
            {
                _symbol = value;
                return this;
            }

            public ApiMoveOrder build()
            {
                return new ApiMoveOrder(_orderId, _newPrice, _uid, _symbol);
            }
        }
    }
    public sealed partial class ApiCancelOrder : ApiCommand, IEquatable<ApiCancelOrder>
    {
        public long OrderId { get; }
        public long Uid { get; }
        public int Symbol { get; }
        public ApiCancelOrder(long orderId, long uid, int symbol)
        {
            OrderId = orderId;
            Uid = uid;
            Symbol = symbol;
        }

        public bool Equals(ApiCancelOrder other)
        {
              return OrderId.Equals(other.OrderId) && Uid.Equals(other.Uid) && Symbol.Equals(other.Symbol);
        }

        public static ApiCancelOrderBuilder Builder()
        {
              return new ApiCancelOrderBuilder();
        }

        public sealed class ApiCancelOrderBuilder
        {
            private long _orderId;
            private long _uid;
            private int _symbol;

            public ApiCancelOrderBuilder orderId(long value)
            {
                _orderId = value;
                return this;
            }
            public ApiCancelOrderBuilder uid(long value)
            {
                _uid = value;
                return this;
            }
            public ApiCancelOrderBuilder symbol(int value)
            {
                _symbol = value;
                return this;
            }

            public ApiCancelOrder build()
            {
                return new ApiCancelOrder(_orderId, _uid, _symbol);
            }
        }
    }
    public sealed partial class ApiReduceOrder : ApiCommand, IEquatable<ApiReduceOrder>
    {
        public long OrderId { get; }
        public long Uid { get; }
        public int Symbol { get; }
        public long ReduceSize { get; }
        public ApiReduceOrder(long orderId, long uid, int symbol, long reduceSize)
        {
            OrderId = orderId;
            Uid = uid;
            Symbol = symbol;
            ReduceSize = reduceSize;
        }

        public bool Equals(ApiReduceOrder other)
        {
              return OrderId.Equals(other.OrderId) && Uid.Equals(other.Uid) && Symbol.Equals(other.Symbol) && ReduceSize.Equals(other.ReduceSize);
        }

        public static ApiReduceOrderBuilder Builder()
        {
              return new ApiReduceOrderBuilder();
        }

        public sealed class ApiReduceOrderBuilder
        {
            private long _orderId;
            private long _uid;
            private int _symbol;
            private long _reduceSize;

            public ApiReduceOrderBuilder orderId(long value)
            {
                _orderId = value;
                return this;
            }
            public ApiReduceOrderBuilder uid(long value)
            {
                _uid = value;
                return this;
            }
            public ApiReduceOrderBuilder symbol(int value)
            {
                _symbol = value;
                return this;
            }
            public ApiReduceOrderBuilder reduceSize(long value)
            {
                _reduceSize = value;
                return this;
            }

            public ApiReduceOrder build()
            {
                return new ApiReduceOrder(_orderId, _uid, _symbol, _reduceSize);
            }
        }
    }
    public sealed partial class ApiAddUser : ApiCommand, IEquatable<ApiAddUser>
    {
        public long Uid { get; }
        public ApiAddUser(long uid)
        {
            Uid = uid;
        }

        public bool Equals(ApiAddUser other)
        {
              return Uid.Equals(other.Uid);
        }

        public static ApiAddUserBuilder Builder()
        {
              return new ApiAddUserBuilder();
        }

        public sealed class ApiAddUserBuilder
        {
            private long _uid;

            public ApiAddUserBuilder uid(long value)
            {
                _uid = value;
                return this;
            }

            public ApiAddUser build()
            {
                return new ApiAddUser(_uid);
            }
        }
    }
    public sealed partial class ApiAdjustUserBalance : ApiCommand, IEquatable<ApiAdjustUserBalance>
    {
        public long Uid { get; }
        public int Currency { get; }
        public long Amount { get; }
        public long TransactionId { get; }
        public ApiAdjustUserBalance(long uid, int currency, long amount, long transactionId)
        {
            Uid = uid;
            Currency = currency;
            Amount = amount;
            TransactionId = transactionId;
        }

        public bool Equals(ApiAdjustUserBalance other)
        {
              return Uid.Equals(other.Uid) && Currency.Equals(other.Currency) && Amount.Equals(other.Amount) && TransactionId.Equals(other.TransactionId);
        }

        public static ApiAdjustUserBalanceBuilder Builder()
        {
              return new ApiAdjustUserBalanceBuilder();
        }

        public sealed class ApiAdjustUserBalanceBuilder
        {
            private long _uid;
            private int _currency;
            private long _amount;
            private long _transactionId;

            public ApiAdjustUserBalanceBuilder uid(long value)
            {
                _uid = value;
                return this;
            }
            public ApiAdjustUserBalanceBuilder currency(int value)
            {
                _currency = value;
                return this;
            }
            public ApiAdjustUserBalanceBuilder amount(long value)
            {
                _amount = value;
                return this;
            }
            public ApiAdjustUserBalanceBuilder transactionId(long value)
            {
                _transactionId = value;
                return this;
            }

            public ApiAdjustUserBalance build()
            {
                return new ApiAdjustUserBalance(_uid, _currency, _amount, _transactionId);
            }
        }
    }
    public sealed partial class ApiBinaryDataCommand : ApiCommand, IEquatable<ApiBinaryDataCommand>
    {
        public int TransferId { get; }
        public BinaryDataCommand Data { get; }
        public ApiBinaryDataCommand(int transferId, BinaryDataCommand data)
        {
            TransferId = transferId;
            Data = data;
        }

        public bool Equals(ApiBinaryDataCommand other)
        {
              return TransferId.Equals(other.TransferId) && Data.Equals(other.Data);
        }

        public static ApiBinaryDataCommandBuilder Builder()
        {
              return new ApiBinaryDataCommandBuilder();
        }

        public sealed class ApiBinaryDataCommandBuilder
        {
            private int _transferId;
            private BinaryDataCommand _data;

            public ApiBinaryDataCommandBuilder transferId(int value)
            {
                _transferId = value;
                return this;
            }
            public ApiBinaryDataCommandBuilder data(BinaryDataCommand value)
            {
                _data = value;
                return this;
            }

            public ApiBinaryDataCommand build()
            {
                return new ApiBinaryDataCommand(_transferId, _data);
            }
        }
    }
    public sealed partial class ApiOrderBookRequest : ApiCommand, IEquatable<ApiOrderBookRequest>
    {
        public int Symbol { get; }
        public int Size { get; }
        public ApiOrderBookRequest(int symbol, int size)
        {
            Symbol = symbol;
            Size = size;
        }

        public bool Equals(ApiOrderBookRequest other)
        {
              return Symbol.Equals(other.Symbol) && Size.Equals(other.Size);
        }

        public static ApiOrderBookRequestBuilder Builder()
        {
              return new ApiOrderBookRequestBuilder();
        }

        public sealed class ApiOrderBookRequestBuilder
        {
            private int _symbol;
            private int _size;

            public ApiOrderBookRequestBuilder symbol(int value)
            {
                _symbol = value;
                return this;
            }
            public ApiOrderBookRequestBuilder size(int value)
            {
                _size = value;
                return this;
            }

            public ApiOrderBookRequest build()
            {
                return new ApiOrderBookRequest(_symbol, _size);
            }
        }
    }
}


				

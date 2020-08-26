using System;
using Exchange.Core.Common;

namespace Exchange.Core.Orderbook
{
    public sealed partial class DirectOrder : IEquatable<DirectOrder>
    {
        public long OrderId { get; set; }
        public long Price { get; set; }
        public long Size { get; set; }
        public long Filled { get; set; }
        public long ReserveBidPrice { get; set; }
        public OrderAction Action { get; set; }
        public long Uid { get; set; }
        public long Timestamp { get; set; }
        public Bucket Parent { get; set; }
        public DirectOrder Next { get; set; }
        public DirectOrder Prev { get; set; }
        public DirectOrder(long orderId, long price, long size, long filled, long reserveBidPrice, OrderAction action, long uid, long timestamp, Bucket parent, DirectOrder next, DirectOrder prev)
        {
            OrderId = orderId;
            Price = price;
            Size = size;
            Filled = filled;
            ReserveBidPrice = reserveBidPrice;
            Action = action;
            Uid = uid;
            Timestamp = timestamp;
            Parent = parent;
            Next = next;
            Prev = prev;
        }

        public bool Equals(DirectOrder other)
        {
              return OrderId.Equals(other.OrderId) && Price.Equals(other.Price) && Size.Equals(other.Size) && Filled.Equals(other.Filled) && ReserveBidPrice.Equals(other.ReserveBidPrice) && Action.Equals(other.Action) && Uid.Equals(other.Uid) && Timestamp.Equals(other.Timestamp) && Parent.Equals(other.Parent) && Next.Equals(other.Next) && Prev.Equals(other.Prev);
        }

        public static DirectOrderBuilder Builder()
        {
              return new DirectOrderBuilder();
        }

        public sealed class DirectOrderBuilder
        {
            private long _orderId;
            private long _price;
            private long _size;
            private long _filled;
            private long _reserveBidPrice;
            private OrderAction _action;
            private long _uid;
            private long _timestamp;
            private Bucket _parent;
            private DirectOrder _next;
            private DirectOrder _prev;

            public DirectOrderBuilder orderId(long value)
            {
                _orderId = value;
                return this;
            }
            public DirectOrderBuilder price(long value)
            {
                _price = value;
                return this;
            }
            public DirectOrderBuilder size(long value)
            {
                _size = value;
                return this;
            }
            public DirectOrderBuilder filled(long value)
            {
                _filled = value;
                return this;
            }
            public DirectOrderBuilder reserveBidPrice(long value)
            {
                _reserveBidPrice = value;
                return this;
            }
            public DirectOrderBuilder action(OrderAction value)
            {
                _action = value;
                return this;
            }
            public DirectOrderBuilder uid(long value)
            {
                _uid = value;
                return this;
            }
            public DirectOrderBuilder timestamp(long value)
            {
                _timestamp = value;
                return this;
            }
            public DirectOrderBuilder parent(Bucket value)
            {
                _parent = value;
                return this;
            }
            public DirectOrderBuilder next(DirectOrder value)
            {
                _next = value;
                return this;
            }
            public DirectOrderBuilder prev(DirectOrder value)
            {
                _prev = value;
                return this;
            }

            public DirectOrder build()
            {
                return new DirectOrder(_orderId, _price, _size, _filled, _reserveBidPrice, _action, _uid, _timestamp, _parent, _next, _prev);
            }
        }
    }
}


				

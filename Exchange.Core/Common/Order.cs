using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common
{
    public sealed partial class Order : IOrder, IWriteBytesMarshallable
    {
        public Order(IBytesIn bytes)
        {
            this.OrderId = bytes.readLong(); // orderId
            this.Price = bytes.readLong();  // price
            this.Size = bytes.readLong(); // size
            this.Filled = bytes.readLong(); // filled
            this.ReserveBidPrice = bytes.readLong(); // price2
            this.Action = (OrderAction)bytes.readByte();
            this.Uid = bytes.readLong(); // uid
            this.Timestamp = bytes.readLong(); // timestamp
                                               //        this.userCookie = bytes.readInt();  // userCookie

        }

        public void writeMarshallable(IBytesOut bytes)
        {
            bytes.writeLong(OrderId);
            bytes.writeLong(Price);
            bytes.writeLong(Size);
            bytes.writeLong(Filled);
            bytes.writeLong(ReserveBidPrice);
            bytes.writeByte((byte)Action);
            bytes.writeLong(Uid);
            bytes.writeLong(Timestamp);
            //        bytes.writeInt(userCookie);
        }

        public override string ToString()
        {
            return "[" + OrderId + " " + (Action == OrderAction.ASK ? 'A' : 'B')
                    + Price + ":" + Size + "F" + Filled
                    // + " C" + userCookie
                    + " U" + Uid + "]";
        }

        public override int GetHashCode()
        {
            //return (int)(97 * OrderId + 
            //    997 * (int)Action +
            //    9997 * Price +
            //    99997 * Size +
            //    99997 * ReserveBidPrice +
            //    999997 * Filled +
            //        //userCookie, timestamp
            //    9999997 * Uid);
            return (int)(97 * OrderId +
    997 * (int)Action +
    9997 * Price +
    99997 * Size +
    999997 * ReserveBidPrice +
    9999997 * Filled +
    //userCookie,
    99999997 * Uid);

        }


        ///**
        // * timestamp is not included into hashCode() and equals() for repeatable results
        // */
        //@Override
        //public boolean equals(Object o)
        //{
        //    if (o == this) return true;
        //    if (o == null) return false;
        //    if (!(o instanceof Order)) return false;

        //    Order other = (Order)o;

        //    // ignore timestamp and userCookie
        //    return orderId == other.orderId
        //            && action == other.action
        //            && price == other.price
        //            && size == other.size
        //            && reserveBidPrice == other.reserveBidPrice
        //            && filled == other.filled
        //            && uid == other.uid;
        //}

        public int stateHash()
        {
            return GetHashCode();
        }
    }
}

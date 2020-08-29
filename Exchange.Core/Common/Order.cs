using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common
{
    public sealed partial class Order : IOrder //: WriteBytesMarshallable
    {
        //public Order(BytesIn bytes)
        //{


        //    this.orderId = bytes.readLong(); // orderId
        //    this.price = bytes.readLong();  // price
        //    this.size = bytes.readLong(); // size
        //    this.filled = bytes.readLong(); // filled
        //    this.reserveBidPrice = bytes.readLong(); // price2
        //    this.action = OrderAction.of(bytes.readByte());
        //    this.uid = bytes.readLong(); // uid
        //    this.timestamp = bytes.readLong(); // timestamp
        //                                       //        this.userCookie = bytes.readInt();  // userCookie

        //}

        //@Override
        //public void writeMarshallable(BytesOut bytes)
        //{
        //    bytes.writeLong(orderId);
        //    bytes.writeLong(price);
        //    bytes.writeLong(size);
        //    bytes.writeLong(filled);
        //    bytes.writeLong(reserveBidPrice);
        //    bytes.writeByte(action.getCode());
        //    bytes.writeLong(uid);
        //    bytes.writeLong(timestamp);
        //    //        bytes.writeInt(userCookie);
        //}

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

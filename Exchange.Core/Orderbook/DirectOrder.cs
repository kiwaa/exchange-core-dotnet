using Exchange.Core.Common;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Orderbook
{
    public partial class DirectOrder : IOrder, IWriteBytesMarshallable
    {

        // public int userCookie;

        public DirectOrder()
        {

        }

        public DirectOrder(IBytesIn bytes)
        {


            this.OrderId = bytes.readLong(); // orderId
            this.Price = bytes.readLong();  // price
            this.Size = bytes.readLong(); // size
            this.Filled = bytes.readLong(); // filled
            this.ReserveBidPrice = bytes.readLong(); // price2
            this.Action = (OrderAction)bytes.readByte();
            this.Uid = bytes.readLong(); // uid
            this.Timestamp = bytes.readLong(); // timestamp
                                               // this.userCookie = bytes.readInt();  // userCookie

            // TODO
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
            // bytes.writeInt(userCookie);
            // TODO
        }

        public override string ToString()
        {
            return "[" + OrderId + " " + (Action == OrderAction.ASK ? 'A' : 'B')
                    + Price + ":" + Size + "F" + Filled
                    // + " C" + userCookie
                    + " U" + Uid + "]";
        }

        //@Override
        //    public int hashCode()
        //{
        //    return Objects.hash(orderId, action, price, size, reserveBidPrice, filled,
        //            //userCookie,
        //            uid);
        //}


        ///**
        // * timestamp is not included into hashCode() and equals() for repeatable results
        // */
        //@Override
        //    public boolean equals(Object o)
        //{
        //    if (o == this) return true;
        //    if (o == null) return false;
        //    if (!(o instanceof DirectOrder)) return false;

        //    DirectOrder other = (DirectOrder)o;

        //    // ignore userCookie && timestamp
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
            return (int)(97 * OrderId +
                997 * (int)Action +
                9997 * Price +
                99997 * Size +
                999997 * ReserveBidPrice +
                9999997 * Filled +
                    //userCookie,
                99999997 * Uid);
        }
    }
}

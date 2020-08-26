using Exchange.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Orderbook
{
    public partial class DirectOrder : IOrder //: WriteBytesMarshallable
    {

        // public int userCookie;

        public DirectOrder()
        {

        }

        //public DirectOrder(BytesIn bytes)
        //{


        //    this.orderId = bytes.readLong(); // orderId
        //    this.price = bytes.readLong();  // price
        //    this.size = bytes.readLong(); // size
        //    this.filled = bytes.readLong(); // filled
        //    this.reserveBidPrice = bytes.readLong(); // price2
        //    this.action = OrderAction.of(bytes.readByte());
        //    this.uid = bytes.readLong(); // uid
        //    this.timestamp = bytes.readLong(); // timestamp
        //                                       // this.userCookie = bytes.readInt();  // userCookie

        //    // TODO
        //}

        //@Override
        //    public void writeMarshallable(BytesOut bytes)
        //{
        //    bytes.writeLong(orderId);
        //    bytes.writeLong(price);
        //    bytes.writeLong(size);
        //    bytes.writeLong(filled);
        //    bytes.writeLong(reserveBidPrice);
        //    bytes.writeByte(action.getCode());
        //    bytes.writeLong(uid);
        //    bytes.writeLong(timestamp);
        //    // bytes.writeInt(userCookie);
        //    // TODO
        //}

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

        //@Override
        //    public int stateHash()
        //{
        //    return Objects.hash(orderId, action, price, size, reserveBidPrice, filled,
        //            //userCookie,
        //            uid);
        //}
    }
}

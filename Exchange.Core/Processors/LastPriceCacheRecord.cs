using Exchange.Core.Common;
using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Processors
{
    public class LastPriceCacheRecord : IStateHash //BytesMarshallable, 
    {
        public long askPrice = long.MaxValue;
        public long bidPrice = 0L;

        public LastPriceCacheRecord()
        {
        }

        public LastPriceCacheRecord(long askPrice, long bidPrice)
        {
            this.askPrice = askPrice;
            this.bidPrice = bidPrice;
        }

        public LastPriceCacheRecord(IBytesIn bytes)
        {
            this.askPrice = bytes.readLong();
            this.bidPrice = bytes.readLong();
        }

        public void writeMarshallable(IBytesOut bytes)
        {
            bytes.writeLong(askPrice);
            bytes.writeLong(bidPrice);
        }

        public LastPriceCacheRecord averagingRecord()
        {
            LastPriceCacheRecord average = new LastPriceCacheRecord();
            average.askPrice = (this.askPrice + this.bidPrice) >> 1;
            average.bidPrice = average.askPrice;
            return average;
        }

        public static LastPriceCacheRecord dummy = new LastPriceCacheRecord(42, 42);

        public int stateHash()
        {
            return (int)(97 * askPrice +
                    997 * bidPrice);
        }
    }


}

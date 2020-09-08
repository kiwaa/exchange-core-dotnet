using OpenHFT.Chronicle.WireMock;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Common.Api.Reports
{
    public class Position : IWriteBytesMarshallable
    {
        public int quoteCurrency { get; set; }
        // open positions state (for margin trades only)
        public PositionDirection direction { get; set; }
        public long openVolume { get; set; }
        public long openPriceSum { get; set; }
        public long profit { get; set; }

        // pending orders total size
        public long pendingSellSize { get; set; }
        public long pendingBuySize { get; set; }

        public Position(IBytesIn bytes)
        {

            this.quoteCurrency = bytes.readInt();

            this.direction = (PositionDirection)bytes.readByte();
            this.openVolume = bytes.readLong();
            this.openPriceSum = bytes.readLong();
            this.profit = bytes.readLong();

            this.pendingSellSize = bytes.readLong();
            this.pendingBuySize = bytes.readLong();
        }

        public Position(int quoteCurrency, PositionDirection direction, long openVolume, long openPriceSum, long profit, long pendingSellSize, long pendingBuySize)
        {
            this.quoteCurrency = quoteCurrency;
            this.direction = direction;
            this.openVolume = openVolume;
            this.openPriceSum = openPriceSum;
            this.profit = profit;
            this.pendingSellSize = pendingSellSize;
            this.pendingBuySize = pendingBuySize;
        }

        public void writeMarshallable(IBytesOut bytes)
        {
            bytes.writeInt(quoteCurrency);
            bytes.writeByte((byte)direction);
            bytes.writeLong(openVolume);
            bytes.writeLong(openPriceSum);
            bytes.writeLong(profit);
            bytes.writeLong(pendingSellSize);
            bytes.writeLong(pendingBuySize);
        }
    }
}

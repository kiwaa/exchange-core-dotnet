using System.Collections.Generic;

namespace Exchange.Core.Common
{
    public sealed class L2MarketData
    {
        public static readonly int L2_SIZE = 32;

        public int AskSize { get; set; }
        public int BidSize { get; set; }

        public long[] AskPrices { get; }
        public long[] AskVolumes { get; }
        public long[] AskOrders { get; }
        public long[] BidPrices { get; }
        public long[] BidVolumes { get; }
        public long[] BidOrders { get; }

        // when published
        public long timestamp { get; }
        public long referenceSeq { get; }

        //    public long totalVolumeAsk;
        //    public long totalVolumeBid;

        public L2MarketData(long[] askPrices, long[] askVolumes, long[] askOrders, long[] bidPrices, long[] bidVolumes, long[] bidOrders)
        {
            AskPrices = askPrices;
            AskVolumes = askVolumes;
            AskOrders = askOrders;
            BidPrices = bidPrices;
            BidVolumes = bidVolumes;
            BidOrders = bidOrders;

            AskSize = askPrices != null ? askPrices.Length : 0;
            BidSize = bidPrices != null ? bidPrices.Length : 0;
        }

        public L2MarketData(int askSize, int bidSize)
        {
            this.AskPrices = new long[askSize];
            this.BidPrices = new long[bidSize];
            this.AskVolumes = new long[askSize];
            this.BidVolumes = new long[bidSize];
            this.AskOrders = new long[askSize];
            this.BidOrders = new long[bidSize];
        }


        public long totalOrderBookVolumeAsk()
        {
            long totalVolume = 0L;
            for (int i = 0; i < AskSize; i++)
            {
                totalVolume += AskVolumes[i];
            }
            return totalVolume;
        }

        public long totalOrderBookVolumeBid()
        {
            long totalVolume = 0L;
            for (int i = 0; i < BidSize; i++)
            {
                totalVolume += BidVolumes[i];
            }
            return totalVolume;
        }


        public override bool Equals(object obj)
        {
            if (!(obj is L2MarketData)) {
                return false;
            }
            L2MarketData o = (L2MarketData)obj;

            if (AskSize != o.AskSize || BidSize != o.BidSize)
            {
                return false;
            }

            for (int i = 0; i < AskSize; i++)
            {
                if (AskPrices[i] != o.AskPrices[i] || AskVolumes[i] != o.AskVolumes[i] || AskOrders[i] != o.AskOrders[i])
                {
                    return false;
                }
            }
            for (int i = 0; i < BidSize; i++)
            {
                if (BidPrices[i] != o.BidPrices[i] || BidVolumes[i] != o.BidVolumes[i] || BidOrders[i] != o.BidOrders[i])
                {
                    return false;
                }
            }
            return true;

        }

        // TODO hashcode
    }
}
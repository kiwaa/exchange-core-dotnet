using Exchange.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Tests
{
    public class L2MarketDataHelper
    {
        private List<long> askPrices;
        private List<long> askVolumes;
        private List<long> askOrders;
        private List<long> bidPrices;
        private List<long> bidVolumes;
        private List<long> bidOrders;

        public L2MarketDataHelper(L2MarketData l2)
        {
            askPrices = l2.AskPrices.ToList();
            askVolumes = l2.AskVolumes.ToList();
            askOrders = l2.AskOrders.ToList();
            bidPrices = l2.BidPrices.ToList();
            bidVolumes = l2.BidVolumes.ToList();
            bidOrders = l2.BidOrders.ToList();
        }

        public L2MarketData build()
        {
            return new L2MarketData(
                    askPrices.ToArray(),
                    askVolumes.ToArray(),
                    askOrders.ToArray(),
                    bidPrices.ToArray(),
                    bidVolumes.ToArray(),
                    bidOrders.ToArray()
            );
        }

        public long aggregateBuyBudget(long size)
        {

            long budget = 0;
            for (int i = 0; i < askPrices.Count; i++)
            {
                long v = askVolumes[i];
                long p = askPrices[i];
                if (v < size)
                {
                    budget += v * p;
                    size -= v;
                }
                else
                {
                    return budget + size * p;
                }
            }

            throw new NotImplementedException("Can not collect size " + size);
            //throw new IllegalArgumentException("Can not collect size " + size);
        }

        public long aggregateSellExpectation(long size)
        {

            long expectation = 0;
            for (int i = 0; i < bidPrices.Count; i++)
            {
                long v = bidVolumes[i];
                long p = bidPrices[i];
                if (v < size)
                {
                    expectation += v * p;
                    size -= v;
                }
                else
                {
                    return expectation + size * p;
                }
            }

            throw new NotImplementedException("Can not collect size " + size);
            //throw new IllegalArgumentException("Can not collect size " + size);
        }

        public L2MarketDataHelper setAskPrice(int pos, int askPrice)
        {
            askPrices[pos] = askPrice;
            return this;
        }

        public L2MarketDataHelper setBidPrice(int pos, int bidPrice)
        {
            bidPrices[pos] = bidPrice;
            return this;
        }

        public L2MarketDataHelper setAskVolume(int pos, long askVolume)
        {
            askVolumes[pos] = askVolume;
            return this;
        }

        public L2MarketDataHelper setBidVolume(int pos, long bidVolume)
        {
            bidVolumes[pos] = bidVolume;
            return this;
        }

        public L2MarketDataHelper decrementAskVolume(int pos, long askVolumeDiff)
        {
            askVolumes[pos] -= askVolumeDiff;
            return this;
        }

        public L2MarketDataHelper decrementBidVolume(int pos, long bidVolumeDiff)
        {
            bidVolumes[pos] -= bidVolumeDiff;
            return this;
        }

        public L2MarketDataHelper setAskPriceVolume(int pos, int askPrice, long askVolume)
        {
            askVolumes[pos] = askVolume;
            askPrices[pos] = askPrice;
            return this;
        }

        public L2MarketDataHelper setBidPriceVolume(int pos, int bidPrice, long bidVolume)
        {
            bidVolumes[pos] = bidVolume;
            bidPrices[pos] = bidPrice;
            return this;
        }

        public L2MarketDataHelper decrementAskOrdersNum(int pos)
        {
            askOrders[pos]--;
            return this;
        }

        public L2MarketDataHelper decrementBidOrdersNum(int pos)
        {
            bidOrders[pos]--;
            return this;
        }

        public L2MarketDataHelper incrementAskOrdersNum(int pos)
        {
            askOrders[pos]++;
            return this;
        }

        public L2MarketDataHelper incrementBidOrdersNum(int pos)
        {
            bidOrders[pos]++;
            return this;
        }

        public L2MarketDataHelper removeAsk(int pos)
        {
            askPrices.RemoveAt(pos);
            askVolumes.RemoveAt(pos);
            askOrders.RemoveAt(pos);
            return this;
        }

        public L2MarketDataHelper removeAllAsks()
        {
            askPrices.Clear();
            askVolumes.Clear();
            askOrders.Clear();
            return this;
        }

        public L2MarketDataHelper removeBid(int pos)
        {
            bidPrices.RemoveAt(pos);
            bidVolumes.RemoveAt(pos);
            bidOrders.RemoveAt(pos);
            return this;
        }

        public L2MarketDataHelper removeAllBids()
        {
            bidPrices.Clear();
            bidVolumes.Clear();
            bidOrders.Clear();
            return this;
        }

        public L2MarketDataHelper insertAsk(int pos, int price, long volume)
        {
            askPrices.Insert(pos, price);
            askVolumes.Insert(pos, volume);
            askOrders.Insert(pos, 1);
            return this;
        }

        public L2MarketDataHelper insertBid(int pos, int price, long volume)
        {
            bidPrices.Insert(pos, price);
            bidVolumes.Insert(pos, volume);
            bidOrders.Insert(pos, 1);
            return this;
        }

        public L2MarketDataHelper addAsk(int price, long volume)
        {
            askPrices.Add(price);
            askVolumes.Add(volume);
            askOrders.Add(1);
            return this;
        }

        public L2MarketDataHelper addBid(int price, long volume)
        {
            bidPrices.Add(price);
            bidVolumes.Add(volume);
            bidOrders.Add(1);
            return this;
        }


        //public String dumpOrderBook(L2MarketData l2MarketData)
        //{

        //    int askSize = l2MarketData.AskSize;
        //    int bidSize = l2MarketData.BidSize;

        //    long[] askPrices = l2MarketData.AskPrices;
        //    long[] askVolumes = l2MarketData.AskVolumes;
        //    long[] askOrders = l2MarketData.AskOrders;
        //    long[] bidPrices = l2MarketData.BidPrices;
        //    long[] bidVolumes = l2MarketData.BidVolumes;
        //    long[] bidOrders = l2MarketData.BidOrders;

        //    int priceWith = maxWidth(2, Arrays.copyOf(askPrices, askSize), Arrays.copyOf(bidPrices, bidSize));
        //    int volWith = maxWidth(2, Arrays.copyOf(askVolumes, askSize), Arrays.copyOf(bidVolumes, bidSize));
        //    int ordWith = maxWidth(2, Arrays.copyOf(askOrders, askSize), Arrays.copyOf(bidOrders, bidSize));

        //    StringBuilder s = new StringBuilder("Order book:\n");
        //    s.append(".").append(Strings.repeat("-", priceWith - 2)).append("ASKS").append(Strings.repeat("-", volWith - 1)).append(".\n");
        //    for (int i = askSize - 1; i >= 0; i--)
        //    {
        //        String price = Strings.padStart(String.valueOf(askPrices[i]), priceWith, ' ');
        //        String volume = Strings.padStart(String.valueOf(askVolumes[i]), volWith, ' ');
        //        String orders = Strings.padStart(String.valueOf(askOrders[i]), ordWith, ' ');
        //        s.append(String.format("|%s|%s|%s|\n", price, volume, orders));
        //    }
        //    s.append("|").append(Strings.repeat("-", priceWith)).append("+").append(Strings.repeat("-", volWith)).append("|\n");
        //    for (int i = 0; i < bidSize; i++)
        //    {
        //        String price = Strings.padStart(String.valueOf(bidPrices[i]), priceWith, ' ');
        //        String volume = Strings.padStart(String.valueOf(bidVolumes[i]), volWith, ' ');
        //        String orders = Strings.padStart(String.valueOf(bidOrders[i]), ordWith, ' ');
        //        s.append(String.format("|%s|%s|%s|\n", price, volume, orders));
        //    }
        //    s.append("'").append(Strings.repeat("-", priceWith - 2)).append("BIDS").append(Strings.repeat("-", volWith - 1)).append("'\n");
        //    return s.toString();
        //}

        //private static int maxWidth(int minWidth, long[]... arrays)
        //{
        //    return Arrays.stream(arrays)
        //            .flatMapToLong(Arrays::stream)
        //            .mapToInt(p->String.valueOf(p).length())
        //            .max()
        //            .orElse(minWidth);
        //}

        //private static int maxWidth(int minWidth, int[]... arrays)
        //{
        //    return Arrays.stream(arrays)
        //            .flatMapToInt(Arrays::stream)
        //            .map(p->String.valueOf(p).length())
        //            .max()
        //            .orElse(minWidth);
        //}


    }

}

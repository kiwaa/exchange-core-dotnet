using Exchange.Core.Orderbook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Utils
{
    public sealed class TestOrdersGeneratorSession
    {

        public readonly IOrderBook orderBook;

        public readonly int transactionsNumber;
        public readonly int targetOrderBookOrdersHalf;

        public readonly long priceDeviation;

        public readonly bool avalancheIOC;

        public readonly int numUsers;
        public readonly Func<int, int> uidMapper;

        public readonly int symbol;

        public readonly Random rand;

        public readonly Dictionary<int,int> orderPrices = new Dictionary<int, int>();
        public readonly Dictionary<int, int> orderSizes = new Dictionary<int, int>();
        public readonly Dictionary<int, int> orderUids = new Dictionary<int, int>();

        public readonly List<int> orderBookSizeAskStat = new List<int>();
        public readonly List<int> orderBookSizeBidStat = new List<int>();
        public readonly List<int> orderBookNumOrdersAskStat = new List<int>();
        public readonly List<int> orderBookNumOrdersBidStat = new List<int>();

        public readonly long minPrice;
        public readonly long maxPrice;

        public readonly int lackOrOrdersFastFillThreshold;

        public long lastTradePrice;

        // set to 1 to make price move up and down
        public int priceDirection;

        public bool initialOrdersPlaced = false;

        public long numCompleted = 0;
        public long numRejected = 0;
        public long numReduced = 0;

        public long counterPlaceMarket = 0;
        public long counterPlaceLimit = 0;
        public long counterCancel = 0;
        public long counterMove = 0;
        public long counterReduce = 0;

        public int seq = 1;

        public int filledAtSeq = -1;

        // statistics (updated every 256 orders)
        public int lastOrderBookOrdersSizeAsk = 0;
        public int lastOrderBookOrdersSizeBid = 0;
        public long lastTotalVolumeAsk = 0;
        public long lastTotalVolumeBid = 0;

        //    public SingleWriterRecorder hdrRecorder = new SingleWriterRecorder(Integer.MAX_VALUE, 2);

        public TestOrdersGeneratorSession(IOrderBook orderBook,
                                          int transactionsNumber,
                                          int targetOrderBookOrdersHalf,
                                          bool avalancheIOC,
                                          int numUsers,
                                          Func<int, int> uidMapper,
                                          int symbol,
                                          bool enableSlidingPrice,
                                          int seed)
        {
            this.orderBook = orderBook;
            this.transactionsNumber = transactionsNumber;
            this.targetOrderBookOrdersHalf = targetOrderBookOrdersHalf;
            this.avalancheIOC = avalancheIOC;
            this.numUsers = numUsers;
            this.uidMapper = uidMapper;
            this.symbol = symbol;
            this.rand = new Random(97 * symbol * -177277 + 997 * (seed * 10037 + 198267));

            int price = (int)Math.Pow(10, 3.3 + rand.NextDouble() * 1.5 + rand.NextDouble() * 1.5);

            this.lastTradePrice = price;
            this.priceDeviation = Math.Min((int)(price * 0.05), 10000);
            this.minPrice = price - priceDeviation * 5;
            this.maxPrice = price + priceDeviation * 5;

            // log.debug("Symbol:{} price={} dev={} range({},{})", symbol, price, priceDeviation, minPrice, maxPrice);

            this.priceDirection = enableSlidingPrice ? 1 : 0;

            this.lackOrOrdersFastFillThreshold = Math.Min(TestOrdersGenerator.CHECK_ORDERBOOK_STAT_EVERY_NTH_COMMAND, targetOrderBookOrdersHalf * 3 / 4);
        }
    }
}

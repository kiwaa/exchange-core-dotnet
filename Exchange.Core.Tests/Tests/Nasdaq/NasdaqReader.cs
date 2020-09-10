//using Exchange.Core.Common.Config;
//using Exchange.Core.Tests.Utils;
//using Exchange.Core.Utils;
//using log4net;
//using NUnit.Framework;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Exchange.Core.Tests.Tests.Nasdaq
//{
//    [TestFixture]
//    public class NasdaqReader
//    {
//        private static ILog log = LogManager.GetLogger(typeof(NasdaqReader));

//        [Test]
//        public void test()
//        {

//            int numUsersSuggested = 1_000_000;

//            int numUsers = BitUtil.findNextPositivePowerOfTwo(numUsersSuggested);
//            int numUsersMask = numUsers - 1;

//            PerformanceConfiguration perfCfg = PerformanceConfiguration.throughputPerformanceBuilder().build();
//            InitialStateConfiguration initStateCfg = InitialStateConfiguration.cleanStart("NASDAQ_TEST");

//            using (ExchangeTestContainer container = ExchangeTestContainer.create(perfCfg, initStateCfg, SerializationConfiguration.DEFAULT))
//            {


//                ExchangeApi api = container.api;


//                // common accounts configuration for all users
//                BitSet currencies = new BitSet();
//                currencies.Set(TestConstants.CURRENECY_USD);

//                List<BitSet> userCurrencies = new List<BitSet>(numUsers + 1);
//                for (int i = 1; i <= numUsers + 1; i++)
//                {
//                    userCurrencies.Add(currencies);
//                }
//                container.userAccountsInit(userCurrencies);


//                ITCH50StatListener statListener = new ITCH50StatListener();
//                ITCH50Parser listener = new ITCH50Parser(statListener);


//                string pathname = "../../nasdaq/01302020.NASDAQ_ITCH50";
//                //          String pathname = "../../nasdaq/20190730.PSX_ITCH_50";
//                //          String pathname = "../../nasdaq/20190730.BX_ITCH_50";

//                ExecutionTime executionTime = new ExecutionTime(d=>log.Debug($"Time: {d}"));
//                BinaryFILE.read(new File(pathname), listener);
//                executionTime.Dispose();

//                statListener.printStat();

//                long totalSymbolMessages = statListener.getSymbolStat().values().stream().mapToLong(c->c.counter).sum();
//                //            counters.forEach((k, v) -> log.debug("{} = {}", k, v));
//                log.Info($"TOTAL: {totalSymbolMessages} troughput = {totalSymbolMessages * 1_000_000_000L / executionTime.ResultNs} TPS");

//                //            symbolPrecisions.forEachKeyValue((symbolId, multiplier) -> {
//                //                int min = symbolRangeMin.get(symbolId);
//                //                int max = symbolRangeMax.get(symbolId);
//                //                log.debug("{} (executionTime{}) {}..{}", symbolNames.get(symbolId).trim(), multiplier, min, max);
//                //            });


//                //            perSymbolCounter.forEachKeyValue((symbolId, messagesStat) -> {
//                //                log.debug("{}:", symbolNames.get(symbolId).trim());
//                //                messagesStat.forEach((k, v) -> log.debug("      {} = {}", k, v));
//                //            });


//                //            log.debug();


//            }

//        }

//        public static int hashToUid(long orderId, int numUsersMask)
//        {
//            long x = ((orderId * 0xcc9e2d51) << 15) * 0x1b873593;
//            return 1 + ((int)(x >> 32 ^ x) & numUsersMask);
//        }

//        public static long convertTime(int high, long low)
//        {
//            return low + (((long)high) << 32);
//        }


//        //    public void validatePrice(long price, int stockLocate) {
//        //        if (price % 100 != 0) { // expect price in cents (original NASDAQ precision is 4 dp)
//        //            throw new IllegalStateException("Unexpected price: " + price + " for " + symbolNames.get(stockLocate));
//        //        }
//        //    }
//    }
//}

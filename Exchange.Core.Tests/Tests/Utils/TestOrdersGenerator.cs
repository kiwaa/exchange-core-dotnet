using Exchange.Core.Common;
using Exchange.Core.Common.Api;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Common.Config;
using Exchange.Core.Orderbook;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Utils
{
    public sealed class TestOrdersGenerator
    {
        private static ILog log = LogManager.GetLogger(typeof(TestOrdersGenerator));

        public static readonly double CENTRAL_MOVE_ALPHA = 0.01;

        public static readonly int CHECK_ORDERBOOK_STAT_EVERY_NTH_COMMAND = 512;

        public static readonly Func<int, int> UID_PLAIN_MAPPER = i => i + 1;

        // TODO allow limiting max volume
        // TODO allow limiting number of opened positions (currently it just grows)
        // TODO use longs for prices (optionally)

        //public static MultiSymbolGenResult generateMultipleSymbols(TestOrdersGeneratorConfig config)
        //{

        //    List<CoreSymbolSpecification> coreSymbolSpecifications = config.CoreSymbolSpecifications;
        //    int totalTransactionsNumber = config.TotalTransactionsNumber;
        //    List<BitSet> usersAccounts = config.UsersAccounts;
        //    int targetOrderBookOrdersTotal = config.TargetOrderBookOrdersTotal;
        //    int seed = config.Seed;

        //    Dictionary<int, GenResult> genResults = new Dictionary<int, GenResult>();

        //    using (ExecutionTime ignore = new ExecutionTime(t => log.Debug($"All test commands generated in {t}")))
        //    {

        //        double[] distribution = createWeightedDistribution(coreSymbolSpecifications.Count, seed);
        //        int quotaLeft = totalTransactionsNumber;
        //        Dictionary<int, Task<GenResult>> futures = new Dictionary<int, Task<GenResult>>();

        //        Action<long> sharedProgressLogger = createAsyncProgressLogger(totalTransactionsNumber + targetOrderBookOrdersTotal);

        //        for (int i = coreSymbolSpecifications.Count - 1; i >= 0; i--)
        //        {
        //            CoreSymbolSpecification spec = coreSymbolSpecifications[i];
        //            int orderBookSizeTarget = (int)(targetOrderBookOrdersTotal * distribution[i] + 0.5);
        //            int commandsNum = (i != 0) ? (int)(totalTransactionsNumber * distribution[i] + 0.5) : Math.Max(quotaLeft, 1);
        //            quotaLeft -= commandsNum;
        //            //                log.debug("{}. Generating symbol {} : commands={} orderBookSizeTarget={} (quotaLeft={})", i, spec.symbolId, commandsNum, orderBookSizeTarget, quotaLeft);

        //            int[] uidsAvailableForSymbol = UserCurrencyAccountsGenerator.createUserListForSymbol(usersAccounts, spec, commandsNum);
        //            int numUsers = uidsAvailableForSymbol.Length;
        //            Func<int, int> uidMapper = idx => uidsAvailableForSymbol[idx];
        //            var symbolFuture = generateCommands(commandsNum, orderBookSizeTarget, numUsers, uidMapper, spec.SymbolId, false, config.AvalancheIOC, sharedProgressLogger, seed);

        //            futures[spec.SymbolId] = Task.FromResult(symbolFuture);
        //        }

        //        foreach (var pair in futures)
        //        {
        //            try
        //            {
        //                genResults[pair.Key] = pair.Value.Result;
        //            }
        //            catch (Exception ex)
        //            {
        //                throw new InvalidOperationException("Exception while generating commands for symbol " + pair.Key, ex);
        //            }
        //        }
        //    }

        //    int benchmarkCmdSize = genResults.Values.Select(genResult => genResult.CommandsBenchmark.Count).Sum();

        //    Task<List<ApiCommand>> apiCommandsFill = mergeCommands(genResults, config.Seed, false, Task.FromResult((List<ApiCommand>)null));
        //    Task<List<ApiCommand>> apiCommandsBenchmark = mergeCommands(genResults, config.Seed, true, apiCommandsFill);

        //    return MultiSymbolGenResult.Builder()
        //            .genResults(genResults)
        //            .apiCommandsFill(apiCommandsFill)
        //            .apiCommandsBenchmark(apiCommandsBenchmark)
        //            .benchmarkCommandsSize(benchmarkCmdSize)
        //            .build();

        //}

        //private static Task<List<ApiCommand>> mergeCommands(
        //        Dictionary<int, GenResult> genResults,
        //        long seed,
        //        bool takeBenchmark,
        //        Task<List<ApiCommand>> runAfterThis)
        //{

        //    List<List<OrderCommand>> commandsLists = genResults.Values
        //            .Select(genResult => takeBenchmark ? genResult.CommandsBenchmark : genResult.CommandsFill)
        //            .ToList();

        //    var tmp = takeBenchmark ? "benchmark" : "preFill";
        //    log.Debug($"Merging {commandsLists.Select(x => x.Count).Sum()} commands for {genResults.Count} symbols ({tmp})...");

        //    List<OrderCommand> merged = RandomCollectionsMerger.mergeCollections(commandsLists, seed);

        //    Task<List<ApiCommand>> resultFuture = runAfterThis.thenApplyAsync(ignore => TestOrdersGenerator.convertToApiCommand(merged));

        //    if (takeBenchmark)
        //    {
        //        resultFuture.thenRunAsync(() => printStatistics(merged));
        //    }

        //    return resultFuture;
        //}

        public static double[] createWeightedDistribution(int size, int seed)
        {
            ParetoDistribution paretoDistribution = new ParetoDistribution(new Random(seed), 0.001, 1.5);
            double[] paretoRaw = Enumerable.Range(0, size).Select(x => paretoDistribution.Sample()).ToArray();

            // normalize
            double sum = paretoRaw.Sum();
            double[] doubles = paretoRaw.Select(x => x / sum).ToArray();
            //        Arrays.stream(doubles).sorted().forEach(d -> log.debug("{}", d));
            return doubles;
        }


        public static Action<long> createAsyncProgressLogger(int totalTransactionsNumber)
        {
            long progressLogInterval = 5_000_000_000L; // 5 sec
            long nextUpdateTime = Stopwatch.GetTimestamp() + progressLogInterval;
            long progress = 0;
            return transactions =>
            {
                Interlocked.Add(ref progress, transactions);
                long whenLogNext = nextUpdateTime;
                long timeNow = Stopwatch.GetTimestamp();
                if (timeNow > whenLogNext)
                {
                    //if (nextUpdateTime.compareAndSet(whenLogNext, timeNow + progressLogInterval)) {
                    if (Interlocked.CompareExchange(ref nextUpdateTime, timeNow + progressLogInterval, whenLogNext) != whenLogNext)
                    {
                        // whichever thread won - it should print progress
                        long done = progress;
                        log.Debug(String.Format("Generating commands progress: %.01f%% done (%d of %d)...",
                                done * 100.0 / totalTransactionsNumber, done, totalTransactionsNumber));
                    }
                }
            };
        }

        // TODO generate ApiCommands (less GC load)
        public static GenResult generateCommands(
                int benchmarkTransactionsNumber,
                int targetOrderBookOrders,
                int numUsers,
                Func<int, int> uidMapper,
     int symbol,
     bool enableSlidingPrice,
     bool avalancheIOC,
     Action<long> asyncProgressConsumer,
     int seed)
        {

            // TODO specify symbol type (for testing exchange-bid-move rejects)
            IOrderBook orderBook = new OrderBookNaiveImpl(TestConstants.SYMBOLSPEC_EUR_USD, LoggingConfiguration.DEFAULT);
            //         IOrderBook orderBook = new OrderBookDirectImpl(SYMBOLSPEC_EUR_USD, ObjectsPool.createDefaultTestPool());

            TestOrdersGeneratorSession session = new TestOrdersGeneratorSession(
                   orderBook,
                   benchmarkTransactionsNumber,
                   targetOrderBookOrders / 2, // asks + bids
                   avalancheIOC,
                   numUsers,
                   uidMapper,
                   symbol,
                   enableSlidingPrice,
                   seed);

            List<OrderCommand> commandsFill = new List<OrderCommand>(targetOrderBookOrders);
            List<OrderCommand> commandsBenchmark = new List<OrderCommand>(benchmarkTransactionsNumber);

            int nextSizeCheck = Math.Min(CHECK_ORDERBOOK_STAT_EVERY_NTH_COMMAND, targetOrderBookOrders + 1);

            int totalCommandsNumber = benchmarkTransactionsNumber + targetOrderBookOrders;

            int lastProgressReported = 0;

            for (int i = 0; i < totalCommandsNumber; i++)
            {

                bool fillInProgress = i < targetOrderBookOrders;

                OrderCommand cmd;

                if (fillInProgress)
                {
                    cmd = generateRandomGtcOrder(session);
                    commandsFill.Add(cmd);
                }
                else
                {
                    cmd = generateRandomOrder(session);
                    commandsBenchmark.Add(cmd);
                }

                cmd.ResultCode = CommandResultCode.VALID_FOR_MATCHING_ENGINE;
                cmd.Symbol = session.symbol;
                //log.debug("{}. {}", i, cmd);

                CommandResultCode resultCode = IOrderBook.processCommand(orderBook, cmd);
                if (resultCode != CommandResultCode.SUCCESS)
                {
                    throw new InvalidOperationException("Unsuccessful result code: " + resultCode + " for " + cmd);
                }

                // process and cleanup matcher events
                cmd.processMatcherEvents(ev => matcherTradeEventEventHandler(session, ev, cmd));
                cmd.MatcherEvent = null;

                if (i >= nextSizeCheck)
                {

                    nextSizeCheck += Math.Min(CHECK_ORDERBOOK_STAT_EVERY_NTH_COMMAND, targetOrderBookOrders + 1);

                    updateOrderBookSizeStat(session);
                }

                if (i % 10000 == 9999)
                {
                    asyncProgressConsumer(i - lastProgressReported);
                    lastProgressReported = i;
                }
            }

            asyncProgressConsumer(totalCommandsNumber - lastProgressReported);

            updateOrderBookSizeStat(session);

            L2MarketData l2MarketData = orderBook.getL2MarketDataSnapshot(int.MaxValue);

            return GenResult.Builder()
                    .commandsBenchmark(commandsBenchmark)
                    .commandsFill(commandsFill)
                    .finalOrderbookHash(orderBook.stateHash())
                    .finalOrderBookSnapshot(l2MarketData)
                    .build();
        }

        private static void updateOrderBookSizeStat(TestOrdersGeneratorSession session)
        {

            int ordersNumAsk = session.orderBook.getOrdersNum(OrderAction.ASK);
            int ordersNumBid = session.orderBook.getOrdersNum(OrderAction.BID);

            // log.debug("ask={}, bif={} seq={} filledAtSeq={}", ordersNumAsk, ordersNumBid, session.seq, session.filledAtSeq);

            // regulating OB size
            session.lastOrderBookOrdersSizeAsk = ordersNumAsk;
            session.lastOrderBookOrdersSizeBid = ordersNumBid;
            //        log.debug("ordersNum:{}", ordersNum);

            if (session.initialOrdersPlaced || session.avalancheIOC)
            {
                L2MarketData l2MarketDataSnapshot = session.orderBook.getL2MarketDataSnapshot(int.MaxValue);
                //                log.debug("{}", dumpOrderBook(l2MarketDataSnapshot));

                if (session.avalancheIOC)
                {
                    session.lastTotalVolumeAsk = l2MarketDataSnapshot.totalOrderBookVolumeAsk();
                    session.lastTotalVolumeBid = l2MarketDataSnapshot.totalOrderBookVolumeBid();
                }

                if (session.initialOrdersPlaced)
                {
                    session.orderBookSizeAskStat.Add(l2MarketDataSnapshot.AskSize);
                    session.orderBookSizeBidStat.Add(l2MarketDataSnapshot.BidSize);
                    session.orderBookNumOrdersAskStat.Add(ordersNumAsk);
                    session.orderBookNumOrdersBidStat.Add(ordersNumBid);
                }
            }
        }

        private static void matcherTradeEventEventHandler(TestOrdersGeneratorSession session, MatcherTradeEvent ev, OrderCommand orderCommand)
        {
            int activeOrderId = (int)orderCommand.OrderId;
            if (ev.EventType == MatcherEventType.TRADE)
            {
                if (ev.ActiveOrderCompleted)
                {
                    session.numCompleted++;
                }
                if (ev.MatchedOrderCompleted)
                {
                    session.orderUids.Remove((int)ev.MatchedOrderId);
                    session.numCompleted++;
                }

                // decrease size (important for reduce operation)
                //if (session.orderSizes.addToValue((int)ev.MatchedOrderId, (int)-ev.Size) < 0)
                if (AddToValue(session.orderSizes, (int)ev.MatchedOrderId, (int)-ev.Size) < 0)
                {
                    throw new InvalidOperationException();
                }

                session.lastTradePrice = Math.Min(session.maxPrice, Math.Max(session.minPrice, ev.Price));

                if (ev.Price <= session.minPrice)
                {
                    session.priceDirection = 1;
                }
                else if (ev.Price >= session.maxPrice)
                {
                    session.priceDirection = -1;
                }

            }
            else if (ev.EventType == MatcherEventType.REJECT)
            {
                session.numRejected++;

                // update order book stat if order get rejected
                // that will trigger generator to issue more limit orders
                updateOrderBookSizeStat(session);

            }
            else if (ev.EventType == MatcherEventType.REDUCE)
            {
                session.numReduced++;

            }
            else
            {
                return;
            }

            // decrease size (important for reduce operation)
            //            if (session.orderSizes.addToValue(activeOrderId, (int)-ev.Size) < 0)
            if (AddToValue(session.orderSizes, activeOrderId, (int)-ev.Size) < 0)
            {
                throw new InvalidOperationException("Incorrect filled size for order " + activeOrderId);
            }

            if (ev.ActiveOrderCompleted)
            {
                session.orderUids.Remove(activeOrderId);
            }
        }


        private static OrderCommand generateRandomOrder(TestOrdersGeneratorSession session)
        {

            Random rand = session.rand;

            // TODO move to lastOrderBookOrdersSize writer method
            int lackOfOrdersAsk = session.targetOrderBookOrdersHalf - session.lastOrderBookOrdersSizeAsk;
            int lackOfOrdersBid = session.targetOrderBookOrdersHalf - session.lastOrderBookOrdersSizeBid;
            if (!session.initialOrdersPlaced && lackOfOrdersAsk <= 0 && lackOfOrdersBid <= 0)
            {
                session.initialOrdersPlaced = true;

                session.counterPlaceMarket = 0;
                session.counterPlaceLimit = 0;
                session.counterCancel = 0;
                session.counterMove = 0;
                session.counterReduce = 0;
            }

            OrderAction action = (rand.Next(4) + session.priceDirection >= 2)
                   ? OrderAction.BID
                   : OrderAction.ASK;

            int lackOfOrders = (action == OrderAction.ASK) ? lackOfOrdersAsk : lackOfOrdersBid;

            bool requireFastFill = session.filledAtSeq == null || lackOfOrders > session.lackOrOrdersFastFillThreshold;

            bool growOrders = lackOfOrders > 0;

            //log.debug("{} growOrders={} requireFastFill={} lackOfOrders({})={}", session.seq, growOrders, requireFastFill, action, lackOfOrders);

            if (session.filledAtSeq == null && !growOrders)
            {
                session.filledAtSeq = session.seq;
                //log.debug("Symbol {} filled at {} (targetOb={} trans={})", session.symbol, session.seq, session.targetOrderBookOrdersHalf, session.transactionsNumber);
            }

            int q = rand.Next(growOrders
                   ? (requireFastFill ? 2 : 10)
                   : 40);

            if (q < 2 || session.orderUids.Count == 0)
            {

                if (growOrders)
                {
                    return generateRandomGtcOrder(session);
                }
                else
                {
                    return generateRandomInstantOrder(session);
                }

            }

            // TODO improve random picking performance (custom hashset implementation?)
            //        long t = System.nanoTime();
            int size = Math.Min(session.orderUids.Count, 512);

            int randPos = rand.Next(size);
            IEnumerator<KeyValuePair<int,int>> iterator = session.orderUids.GetEnumerator();

            iterator.MoveNext();
            for (int i = 0; i < randPos; i++)
            {
                iterator.MoveNext();
            }
            KeyValuePair<int, int> rec = iterator.Current;
            //        session.hdrRecorder.recordValue(Math.min(System.nanoTime() - t, Integer.MAX_VALUE));
            int orderId = rec.Key;

            int uid = rec.Value;
            if (uid == 0)
            {
                throw new InvalidOperationException();
            }

            if (q == 2)
            {
                session.orderUids.Remove(orderId);
                session.counterCancel++;
                return OrderCommand.cancel(orderId, uid);

            }
            else if (q == 3)
            {
                session.counterReduce++;

                int prevSize = session.orderSizes[orderId];
                int reduceBy = session.rand.Next(prevSize) + 1;
                return OrderCommand.reduce(orderId, uid, reduceBy);

            }
            else
            {
                int prevPrice = session.orderPrices[orderId];
                if (prevPrice == 0)
                {
                    throw new InvalidOperationException();
                }

                double priceMove = (session.lastTradePrice - prevPrice) * CENTRAL_MOVE_ALPHA;
                int priceMoveRounded;
                if (prevPrice > session.lastTradePrice)
                {
                    priceMoveRounded = (int)Math.Floor(priceMove);
                }
                else if (prevPrice < session.lastTradePrice)
                {
                    priceMoveRounded = (int)Math.Ceiling(priceMove);
                }
                else
                {
                    priceMoveRounded = rand.Next(2) * 2 - 1;
                }

                int newPrice = Math.Min(prevPrice + priceMoveRounded, (int)session.maxPrice);
                // todo add min limit

                // log.debug("session.seq={} orderId={} size={} p={}", session.seq, orderId, session.actualOrders.size(), priceMoveRounded);

                session.counterMove++;

                session.orderPrices[orderId] = newPrice;

                return OrderCommand.update(orderId, (int)(long)uid, newPrice);
            }
        }

        private static OrderCommand generateRandomGtcOrder(TestOrdersGeneratorSession session)
        {

            Random rand = session.rand;

            OrderAction action = (rand.Next(4) + session.priceDirection >= 2) ? OrderAction.BID : OrderAction.ASK;
            int uid = session.uidMapper(rand.Next(session.numUsers));
            int newOrderId = session.seq;

            int dev = 1 + (int)(Math.Pow(rand.NextDouble(), 2) * session.priceDeviation);

            long p = 0;
            int x = 4;
            for (int i = 0; i < x; i++)
            {
                p += rand.Next(dev);
            }
            p = p / x * 2 - dev;
            if (p > 0 ^ action == OrderAction.ASK)
            {
                p = -p;
            }

            //log.debug("p={} action={}", p, action);
            int price = (int)session.lastTradePrice + (int)p;

            int size = 1 + rand.Next(6) * rand.Next(6) * rand.Next(6);


            session.orderPrices[newOrderId] = price;
            session.orderSizes[newOrderId] = size;
            session.orderUids[newOrderId] = uid;
            session.counterPlaceLimit++;
            session.seq++;

            return OrderCommand.Builder()
                    .command(OrderCommandType.PLACE_ORDER)
                    .uid(uid)
                    .orderId(newOrderId)
                    .action(action)
                    .orderType(OrderType.GTC)
                    .size(size)
                    .price(price)
                    .reserveBidPrice(action == OrderAction.BID ? session.maxPrice : 0)// set limit price
                    .build();
        }

        private static OrderCommand generateRandomInstantOrder(TestOrdersGeneratorSession session)
        {

            Random rand = session.rand;

            OrderAction action = (rand.Next(4) + session.priceDirection >= 2) ? OrderAction.BID : OrderAction.ASK;

            int uid = session.uidMapper(rand.Next(session.numUsers));

            int newOrderId = session.seq;

            long priceLimit = action == OrderAction.BID ? session.maxPrice : session.minPrice;

            long size;
            OrderType orderType;
            long priceOrBudget;
            long reserveBidPrice;

            if (session.avalancheIOC)
            {

                // just match with available liquidity

                orderType = OrderType.IOC;
                priceOrBudget = priceLimit;
                reserveBidPrice = action == OrderAction.BID ? session.maxPrice : 0; // set limit price
                long availableVolume = action == OrderAction.ASK ? session.lastTotalVolumeAsk : session.lastTotalVolumeBid;

                long bigRand = rand.NextLong();
                bigRand = bigRand < 0 ? -1 - bigRand : bigRand;
                size = 1 + bigRand % (availableVolume + 1);

                if (action == OrderAction.ASK)
                {
                    session.lastTotalVolumeAsk = Math.Max(session.lastTotalVolumeAsk - size, 0);
                }
                else
                {
                    session.lastTotalVolumeBid = Math.Max(session.lastTotalVolumeAsk - size, 0);
                }
                //                    log.debug("huge size={} at {}", placeCmd.size, session.seq);

            }
            else if (rand.Next(32) == 0)
            {
                // IOC:FOKB = 31:1
                orderType = OrderType.FOK_BUDGET;
                size = 1 + rand.Next(8) * rand.Next(8) * rand.Next(8);

                // set budget-expectation
                priceOrBudget = size * priceLimit;
                reserveBidPrice = priceOrBudget;
            }
            else
            {
                orderType = OrderType.IOC;
                priceOrBudget = priceLimit;
                reserveBidPrice = action == OrderAction.BID ? session.maxPrice : 0; // set limit price
                size = 1 + rand.Next(6) * rand.Next(6) * rand.Next(6);
            }


            session.orderSizes[newOrderId] = (int)size;
            session.counterPlaceMarket++;
            session.seq++;


            return OrderCommand.Builder()
                    .command(OrderCommandType.PLACE_ORDER)
                    .orderType(orderType)
                    .uid(uid)
                    .orderId(newOrderId)
                    .action(action)
                    .size(size)
                    .price(priceOrBudget)
                    .reserveBidPrice(reserveBidPrice)
                    .build();
        }

        public static List<ApiCommand> convertToApiCommand(GenResult genResult)
        {
            List<OrderCommand> commands = new List<OrderCommand>(genResult.CommandsFill);
            commands.AddRange(genResult.CommandsBenchmark);
            return convertToApiCommand(commands, 0, commands.Count);
        }

        public static List<ApiCommand> convertToApiCommand(List<OrderCommand> commands)
        {
            return convertToApiCommand(commands, 0, commands.Count);
        }

        public static List<ApiCommand> convertToApiCommand(List<OrderCommand> commands, int from, int to)
        {
            using (ExecutionTime ignore = new ExecutionTime(t => log.Debug($"Converted {to - from} commands to API commands in: {t}")))
            {
                List<ApiCommand> apiCommands = new List<ApiCommand>(to - from);
                for (int i = from; i < to; i++)
                {
                    OrderCommand cmd = commands[i];
                    switch (cmd.Command)
                    {
                        case OrderCommandType.PLACE_ORDER:
                            apiCommands.Add(ApiPlaceOrder.Builder().symbol(cmd.Symbol).uid(cmd.Uid).orderId(cmd.OrderId).price(cmd.Price)
                                    .size(cmd.Size).action(cmd.Action).orderType(cmd.OrderType).reservePrice(cmd.ReserveBidPrice).build());
                            break;

                        case OrderCommandType.MOVE_ORDER:
                            apiCommands.Add(new ApiMoveOrder(cmd.OrderId, cmd.Price, cmd.Uid, cmd.Symbol));
                            break;

                        case OrderCommandType.CANCEL_ORDER:
                            apiCommands.Add(new ApiCancelOrder(cmd.OrderId, cmd.Uid, cmd.Symbol));
                            break;

                        case OrderCommandType.REDUCE_ORDER:
                            apiCommands.Add(new ApiReduceOrder(cmd.OrderId, cmd.Uid, cmd.Symbol, cmd.Size));
                            break;

                        default:
                            throw new InvalidOperationException("unsupported type: " + cmd.Command);
                    }
                }

                return apiCommands;
            }
        }

        //private static void printStatistics(List<OrderCommand> allCommands)
        //{
        //    int counterPlaceIOC = 0;
        //    int counterPlaceGTC = 0;
        //    int counterPlaceFOKB = 0;
        //    int counterCancel = 0;
        //    int counterMove = 0;
        //    int counterReduce = 0;
        //    Dictionary<int, int> symbolCounters = new Dictionary<int, int>();

        //    foreach (OrderCommand cmd in allCommands)
        //    {
        //        switch (cmd.Command)
        //        {
        //            case OrderCommandType.MOVE_ORDER:
        //                counterMove++;
        //                break;

        //            case OrderCommandType.CANCEL_ORDER:
        //                counterCancel++;
        //                break;

        //            case OrderCommandType.REDUCE_ORDER:
        //                counterReduce++;
        //                break;

        //            case OrderCommandType.PLACE_ORDER:
        //                if (cmd.OrderType == OrderType.IOC)
        //                {
        //                    counterPlaceIOC++;
        //                }
        //                else if (cmd.OrderType == OrderType.GTC)
        //                {
        //                    counterPlaceGTC++;
        //                }
        //                else if (cmd.OrderType == OrderType.FOK_BUDGET)
        //                {
        //                    counterPlaceFOKB++;
        //                }
        //                break;
        //        }
        //        //symbolCounters.addToValue(cmd.Symbol, 1);
        //        AddToValue(symbolCounters, cmd.Symbol, 1);
        //    }

        //    int commandsListSize = allCommands.Count;
        //    IntSummaryStatistics symbolStat = symbolCounters.summaryStatistics();

        //    String commandsGtc = String.Format("%.2f%%", (float)counterPlaceGTC / (float)commandsListSize * 100.0f);
        //    String commandsIoc = String.Format("%.2f%%", (float)counterPlaceIOC / (float)commandsListSize * 100.0f);
        //    String commandsFokb = String.Format("%.2f%%", (float)counterPlaceFOKB / (float)commandsListSize * 100.0f);
        //    String commandsCancel = String.Format("%.2f%%", (float)counterCancel / (float)commandsListSize * 100.0f);
        //    String commandsMove = String.Format("%.2f%%", (float)counterMove / (float)commandsListSize * 100.0f);
        //    String commandsReduce = String.Format("%.2f%%", (float)counterReduce / (float)commandsListSize * 100.0f);
        //    log.Info($"GTC:{commandsGtc} IOC:{commandsIoc} FOKB:{commandsFokb} cancel:{commandsCancel} move:{commandsMove} reduce:{commandsReduce}");

        //    String cpsMax = String.Format("%d (%.2f%%)", symbolStat.getMax(), symbolStat.getMax() * 100.0f / commandsListSize);
        //    String cpsAvg = String.Format("%d (%.2f%%)", (int)symbolStat.getAverage(), symbolStat.getAverage() * 100.0f / commandsListSize);
        //    String cpsMin = String.Format("%d (%.2f%%)", symbolStat.getMin(), symbolStat.getMin() * 100.0f / commandsListSize);
        //    log.Info($"commands per symbol: max:{cpsMax}; avg:{cpsAvg}; min:{cpsMin}");
        //}

        private static int AddToValue(Dictionary<int, int> dict, int key, int value)
        {
            return dict[key] += value;
        }
    }
}

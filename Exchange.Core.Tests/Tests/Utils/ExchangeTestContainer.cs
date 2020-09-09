using Exchange.Core.Common;
using Exchange.Core.Common.Api;
using Exchange.Core.Common.Api.Binary;
using Exchange.Core.Common.Api.Reports;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Common.Config;
using Exchange.Core.Tests.Utils;
using Exchange.Core.Utils;
using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Utils
{
    public sealed class ExchangeTestContainer : IDisposable
    {
        private static ILog log = LogManager.GetLogger(typeof(ExchangeTestContainer));

        private readonly ExchangeCore exchangeCore;
        public ExchangeApi api { get; }
        //private readonly AffinityThreadFactory threadFactory { get; }

        private long uniqueIdCounterLong = 0;
        private volatile int uniqueIdCounterInt = 0;

        public Action<OrderCommand, long> consumer { get; set; } = (cmd, seq) => { };

        public static readonly Action<OrderCommand> CHECK_SUCCESS = cmd => Assert.AreEqual(CommandResultCode.SUCCESS, cmd.ResultCode);

        //public static String timeBasedExchangeId()
        //{
        //    return String.Format("%012X", System.currentTimeMillis());
        //}

        public static ExchangeTestContainer create(PerformanceConfiguration perfCfg)
        {
            return new ExchangeTestContainer(perfCfg,
                    InitialStateConfiguration.CLEAN_TEST,
                    SerializationConfiguration.DEFAULT);
        }

        //public static ExchangeTestContainer create(PerformanceConfiguration perfCfg,
        //                                           InitialStateConfiguration initStateCfg,
        //                                           SerializationConfiguration serializationCfg)
        //{
        //    return new ExchangeTestContainer(perfCfg, initStateCfg, serializationCfg);
        //}

        //public static TestDataFutures prepareTestDataAsync(TestDataParameters parameters, int seed)
        //{
        //    var cssr = generateRandomSymbols(parameters.NumSymbols, parameters.CurrenciesAllowed, parameters.AllowedSymbolTypes);
        //    Task<List<CoreSymbolSpecification>> coreSymbolSpecificationsFuture = Task.FromResult(cssr);

        //    var uar = UserCurrencyAccountsGenerator.generateUsers(parameters.NumAccounts, parameters.CurrenciesAllowed);
        //    Task<List<BitSet>> usersAccountsFuture = Task.FromResult(uar);

        //    var grr = TestOrdersGenerator.generateMultipleSymbols(
        //                    TestOrdersGeneratorConfig.Builder()
        //                            .coreSymbolSpecifications(cssr)
        //                            .totalTransactionsNumber(parameters.TotalTransactionsNumber)
        //                            .usersAccounts(uar)
        //                            .targetOrderBookOrdersTotal(parameters.TargetOrderBookOrdersTotal)
        //                            .seed(seed)
        //                            .preFillMode(parameters.PreFillMode)
        //                            .avalancheIOC(parameters.AvalancheIOC)
        //                            .build());
        //    Task<MultiSymbolGenResult> genResultFuture = Task.FromResult(grr);

        //    return TestDataFutures.Builder()
        //            .coreSymbolSpecifications(coreSymbolSpecificationsFuture)
        //            .usersAccounts(usersAccountsFuture)
        //            .genResult(genResultFuture)
        //            .build();
        //}

        private ExchangeTestContainer(PerformanceConfiguration perfCfg,
                                      InitialStateConfiguration initStateCfg,
                                      SerializationConfiguration serializationCfg)
        {

            //log.debug("CREATING exchange container");

            //this.threadFactory = new AffinityThreadFactory(AffinityThreadFactory.ThreadAffinityMode.THREAD_AFFINITY_ENABLE_PER_PHYSICAL_CORE);

            ExchangeConfiguration exchangeConfiguration = ExchangeConfiguration.defaultBuilder()
                    .initStateCfg(initStateCfg)
                    .performanceCfg(perfCfg)
                    .reportsQueriesCfg(ReportsQueriesConfiguration.createStandardConfig())
                    .ordersProcessingCfg(OrdersProcessingConfiguration.DEFAULT)
                    .loggingCfg(LoggingConfiguration.DEFAULT)
                    .serializationCfg(serializationCfg)
                    .build();

            this.exchangeCore = ExchangeCore.Builder()
                    .resultsConsumer((cmd,seq) => consumer(cmd, seq))
                    .exchangeConfiguration(exchangeConfiguration)
                    .build();

            //log.debug("STARTING exchange container");
            this.exchangeCore.startup();

            //log.debug("STARTED exchange container");
            this.api = this.exchangeCore.getApi();
        }

        public void initBasicSymbols()
        {

            addSymbol(TestConstants.SYMBOLSPEC_EUR_USD);
            addSymbol(TestConstants.SYMBOLSPEC_ETH_XBT);
        }

        public void initFeeSymbols()
        {
            addSymbol(TestConstants.SYMBOLSPECFEE_XBT_LTC);
            addSymbol(TestConstants.SYMBOLSPECFEE_USD_JPY);
        }

        public void initBasicUsers()
        {
            initBasicUser(TestConstants.UID_1);
            initBasicUser(TestConstants.UID_2);
            initBasicUser(TestConstants.UID_3);
            initBasicUser(TestConstants.UID_4);
        }

        public void initFeeUsers()
        {
            initFeeUser(TestConstants.UID_1);
            initFeeUser(TestConstants.UID_2);
            initFeeUser(TestConstants.UID_3);
            initFeeUser(TestConstants.UID_4);
        }

        public void initBasicUser(long uid)
        {
            Assert.AreEqual(api.submitCommandAsync(ApiAddUser.Builder().uid(uid).build()).Result, CommandResultCode.SUCCESS);
            Assert.AreEqual(api.submitCommandAsync(ApiAdjustUserBalance.Builder().uid(uid).transactionId(1L).amount(10_000_00L).currency(TestConstants.CURRENECY_USD).build()).Result, CommandResultCode.SUCCESS);
            Assert.AreEqual(api.submitCommandAsync(ApiAdjustUserBalance.Builder().uid(uid).transactionId(2L).amount(1_0000_0000L).currency(TestConstants.CURRENECY_XBT).build()).Result, CommandResultCode.SUCCESS);
            Assert.AreEqual(api.submitCommandAsync(ApiAdjustUserBalance.Builder().uid(uid).transactionId(3L).amount(1_0000_0000L).currency(TestConstants.CURRENECY_ETH).build()).Result, CommandResultCode.SUCCESS);
        }

        public void initFeeUser(long uid)
        {
            Assert.AreEqual(api.submitCommandAsync(ApiAddUser.Builder().uid(uid).build()).Result, CommandResultCode.SUCCESS);
            Assert.AreEqual(api.submitCommandAsync(ApiAdjustUserBalance.Builder().uid(uid).transactionId(1L).amount(10_000_00L).currency(TestConstants.CURRENECY_USD).build()).Result, CommandResultCode.SUCCESS);
            Assert.AreEqual(api.submitCommandAsync(ApiAdjustUserBalance.Builder().uid(uid).transactionId(2L).amount(10_000_000L).currency(TestConstants.CURRENECY_JPY).build()).Result, CommandResultCode.SUCCESS);
            Assert.AreEqual(api.submitCommandAsync(ApiAdjustUserBalance.Builder().uid(uid).transactionId(3L).amount(1_0000_0000L).currency(TestConstants.CURRENECY_XBT).build()).Result, CommandResultCode.SUCCESS);
            Assert.AreEqual(api.submitCommandAsync(ApiAdjustUserBalance.Builder().uid(uid).transactionId(4L).amount(1000_0000_0000L).currency(TestConstants.CURRENECY_LTC).build()).Result, CommandResultCode.SUCCESS);
        }

        public void createUserWithMoney(long uid, int currency, long amount)
        {
            List<ApiCommand> cmds = new List<ApiCommand>();
            cmds.Add(ApiAddUser.Builder().uid(uid).build());
            cmds.Add(ApiAdjustUserBalance.Builder().uid(uid).transactionId(getRandomTransactionId()).amount(amount).currency(currency).build());
            api.submitCommandsSync(cmds);
        }

        public void addMoneyToUser(long uid, int currency, long amount)
        {
            List<ApiCommand> cmds = new List<ApiCommand>();
            cmds.Add(ApiAdjustUserBalance.Builder().uid(uid).transactionId(getRandomTransactionId()).amount(amount).currency(currency).build());
            api.submitCommandsSync(cmds);
        }


        public void addSymbol(CoreSymbolSpecification symbol)
        {
            sendBinaryDataCommandSync(new BatchAddSymbolsCommand(symbol), 5000);
        }

        //public void addSymbols(List<CoreSymbolSpecification> symbols)
        //{
        //    // split by chunks
        //    Lists.partition(symbols, 10000).forEach(partition => sendBinaryDataCommandSync(new BatchAddSymbolsCommand(partition), 5000));
        //}

        public void sendBinaryDataCommandSync(IBinaryDataCommand data, int timeOutMs)
        {
            Task<CommandResultCode> future = api.submitBinaryDataAsync(data);
            try
            {
                future.Wait(timeOutMs);
                Assert.AreEqual(future.Result, CommandResultCode.SUCCESS);
            }
            catch (AggregateException ex)
            {
                log.Error("Failed sending binary data command", ex);
                throw new Exception("", ex);
            }
        }

        private int getRandomTransferId()
        {
            return Interlocked.Increment(ref uniqueIdCounterInt);
        }

        private long getRandomTransactionId()
        {
            return Interlocked.Increment(ref uniqueIdCounterLong);
        }

        //public void userAccountsInit(List<BitSet> userCurrencies)
        //{

        //    // calculate max amount can transfer to each account so that it is not possible to get long overflow
        //    IntLongHashMap accountsNumPerCurrency = new IntLongHashMap();
        //    userCurrencies.forEach(accounts->accounts.stream().forEach(currency => accountsNumPerCurrency.addToValue(currency, 1)));
        //    IntLongHashMap amountPerAccount = new IntLongHashMap();
        //    accountsNumPerCurrency.forEachKeyValue((currency, numAcc) => amountPerAccount.put(currency, long.MaxValue / (numAcc + 1)));
        //    // amountPerAccount.forEachKeyValue((k, v) -> log.debug("{}={}", k, v));

        //    createUserAccountsRegular(userCurrencies, amountPerAccount);
        //}


        //private void createUserAccountsRegular(List<BitSet> userCurrencies, IntLongHashMap amountPerAccount)
        //{
        //    int numUsers = userCurrencies.size() - 1;

        //    foreach (var uid in Enumerable.Range(1, numUsers))
        //    {
        //        api.submitCommand(ApiAddUser.Builder().uid(uid).build());
        //        userCurrencies.get(uid).stream().forEach(currency =>
        //                api.submitCommand(ApiAdjustUserBalance.builder()
        //                        .uid(uid)
        //                        .transactionId(getRandomTransactionId())
        //                        .amount(amountPerAccount.get(currency))
        //                        .currency(currency)
        //                        .build()));
        //    }

        //    api.submitCommandAsync(ApiNop.builder().build()).Result;
        //}

        public void usersInit(int numUsers, HashSet<int> currencies)
        {
            foreach (var uid in Enumerable.Range(1, numUsers))
            {
                api.submitCommand(ApiAddUser.Builder().uid(uid).build());
                long transactionId = 1L;
                foreach (int currency in currencies)
                {
                    api.submitCommand(ApiAdjustUserBalance.Builder()
                            .uid(uid)
                            .transactionId(transactionId++)
                            .amount(10_0000_0000L)
                            .currency(currency).build());
                }
            }

            api.submitCommandAsync(ApiNop.Builder().build()).Wait();
        }

        //public void resetExchangeCore()
        //{
        //    CommandResultCode res = api.submitCommandAsync(ApiReset.builder().build()).Result;
        //    Assert.AreEqual(res, CommandResultCode.SUCCESS);
        //}

        public void submitCommandSync(ApiCommand apiCommand, CommandResultCode expectedResultCode)
        {
            Assert.AreEqual(api.submitCommandAsync(apiCommand).Result, expectedResultCode);
        }

        public void submitCommandSync(ApiCommand apiCommand, Action<OrderCommand> validator)
        {
            var result = api.submitCommandAsyncFullResponse(apiCommand).Result;
            validator(result);
        }

        public L2MarketData requestCurrentOrderBook(int symbol)
        {
            return api.requestOrderBookAsync(symbol, -1).Result;
        }

        // todo rename
        public void validateUserState(long uid, Action<SingleUserReportResult> resultValidator)
        {
            var result = getUserProfile(uid);
            resultValidator(result);
        }

        public SingleUserReportResult getUserProfile(long clientId)
        {
            return api.processReport(new SingleUserReportQuery(clientId), getRandomTransferId()).Result;
        }

        public TotalCurrencyBalanceReportResult totalBalanceReport()
        {
            TotalCurrencyBalanceReportResult res = api.processReport(new TotalCurrencyBalanceReportQuery(), getRandomTransferId()).Result;
            Dictionary<int,long> openInterestLong = res.OpenInterestLong;
            Dictionary<int, long> openInterestShort = res.OpenInterestShort;
            Dictionary<int, long> openInterestDiff = new Dictionary<int, long>(openInterestLong);

            foreach (var pair in openInterestShort)
                openInterestDiff[pair.Key] += -pair.Value;
            if (openInterestDiff.Any(vol => vol.Value != 0))
            {
                throw new InvalidOperationException("Open Interest balance check failed");
            }

            return res;
        }

        //public int requestStateHash()
        //{
        //    return api.processReport(new StateHashReportQuery(), getRandomTransferId()).get().getStateHash();
        //}

        //public static List<CoreSymbolSpecification> generateRandomSymbols(int num,
        //                                                                  IEnumerable<int> currenciesAllowed,
        //                                                                  AllowedSymbolTypes allowedSymbolTypes)
        //{
        //    Random random = new Random(1);

        //    Func<SymbolType> symbolTypeSupplier;

        //    switch (allowedSymbolTypes)
        //    {
        //        case AllowedSymbolTypes.FUTURES_CONTRACT:
        //            symbolTypeSupplier = () => SymbolType.FUTURES_CONTRACT;
        //            break;

        //        case AllowedSymbolTypes.CURRENCY_EXCHANGE_PAIR:
        //            symbolTypeSupplier = () => SymbolType.CURRENCY_EXCHANGE_PAIR;
        //            break;

        //        case AllowedSymbolTypes.BOTH:
        //        default:
        //            symbolTypeSupplier = () => random.nextBoolean() ? SymbolType.FUTURES_CONTRACT : SymbolType.CURRENCY_EXCHANGE_PAIR;
        //            break;
        //    }

        //    List<int> currencies = new List<int>(currenciesAllowed);
        //    List<CoreSymbolSpecification> result = new List<CoreSymbolSpecification>();
        //    for (int i = 0; i < num;)
        //    {
        //        int baseCurrency = currencies[random.Next(currencies.Count)];
        //        int quoteCurrency = currencies.[random.Next(currencies.Count)];
        //        if (baseCurrency != quoteCurrency)
        //        {
        //            SymbolType type = symbolTypeSupplier();
        //            long makerFee = random.Next(1000);
        //            long takerFee = makerFee + random.Next(500);
        //            CoreSymbolSpecification symbol = CoreSymbolSpecification.Builder()
        //                    .symbolId(TestConstants.SYMBOL_AUTOGENERATED_RANGE_START + i)
        //                    .type(type)
        //                    .baseCurrency(baseCurrency) // TODO for futures can be any value
        //                    .quoteCurrency(quoteCurrency)
        //                    .baseScaleK(100)
        //                    .quoteScaleK(10)
        //                    .takerFee(takerFee)
        //                    .makerFee(makerFee)
        //                    .build();

        //            result.Add(symbol);

        //            //log.debug("{}", symbol);
        //            i++;
        //        }
        //    }
        //    return result;
        //}

        //public void loadSymbolsUsersAndPrefillOrders(TestDataFutures testDataFutures)
        //{

        //    // load symbols
        //    List<CoreSymbolSpecification> coreSymbolSpecifications = testDataFutures.CoreSymbolSpecifications.Result;
        //    log.Info($"Loading {coreSymbolSpecifications.Count} symbols...");
        //    using (ExecutionTime ignore = new ExecutionTime(t => log.Debug($"Loaded all symbols in {t}")))
        //    {
        //        addSymbols(coreSymbolSpecifications);
        //    }

        //    // create accounts and deposit initial funds
        //    List<BitSet> userAccounts = testDataFutures.UsersAccounts.Result;
        //    log.Info($"Loading {userAccounts.Count} users having {userAccounts.Sum(x => x.Cardinality())} accounts...");
        //    using (ExecutionTime ignore = new ExecutionTime(t => log.Debug($"Loaded all users in {t}")))
        //    {
        //        userAccountsInit(userAccounts);
        //    }

        //    List<ApiCommand> apiCommandsFill = testDataFutures.GenResult.Result.getApiCommandsFill().Result;
        //    log.Info($"Order books pre-fill with {apiCommandsFill.Count} orders...");
        //    using (ExecutionTime ignore = new ExecutionTime(t->log.debug("Order books pre-fill completed in {}", t)))
        //    {
        //        getApi().submitCommandsSync(apiCommandsFill);
        //    }

        //    Assert.AreEqual(totalBalanceReport().isGlobalBalancesAllZero(), true);
        //}

        //public void loadSymbolsUsersAndPrefillOrdersNoLog(TestDataFutures testDataFutures)
        //{

        //    // load symbols
        //    addSymbols(testDataFutures.CoreSymbolSpecifications.Result);

        //    // create accounts and deposit initial funds
        //    userAccountsInit(testDataFutures.UsersAccounts.Result);

        //    getApi().submitCommandsSync(testDataFutures.GenResult.Result.getApiCommandsFill().Result);
        //}


        ///**
        // * Run test using threads factory.
        // * This is needed for correct cpu pinning.
        // *
        // * @param test - test lambda
        // * @param <V>  return parameter type
        // * @return result from test lambda
        // */
        //public V executeTestingThread<V>(Callable<V> test)
        //{
        //    try
        //    {
        //        ExecutorService executor = Executors.newSingleThreadExecutor(threadFactory);
        //        V result = executor.submit(test).get();
        //        executor.shutdown();
        //        executor.awaitTermination(3000, TimeUnit.SECONDS);
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new RuntimeException(ex);
        //    }
        //}

        //public float executeTestingThreadPerfMtps(Callable<int> test)
        //{
        //    return executeTestingThread(() =>
        //    {
        //        long tStart = System.currentTimeMillis();
        //        int numMessages = test.call();
        //        long tDuration = System.currentTimeMillis() - tStart;
        //        return numMessages / (float)tDuration / 1000.0f;
        //    });
        //}

        //public float benchmarkMtps(List<ApiCommand> apiCommandsBenchmark)
        //{
        //    long tStart = System.currentTimeMillis();
        //    getApi().submitCommandsSync(apiCommandsBenchmark);
        //    long tDuration = System.currentTimeMillis() - tStart;
        //    return apiCommandsBenchmark.size() / (float)tDuration / 1000.0f;
        //}

        public void Dispose()
        {
            exchangeCore.shutdown(TimeSpan.FromMilliseconds(3000));
        }


    }
}

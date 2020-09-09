using Exchange.Core.Common.Config;
using Exchange.Core.Tests.Utils;
using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Tests.Utils
{
    public class ThroughputTestsModule
    {
        private static ILog log = LogManager.GetLogger(typeof(ThroughputTestsModule));

        public static void throughputTestImpl(PerformanceConfiguration performanceCfg,
                                              TestDataParameters testDataParameters,
                                              InitialStateConfiguration initialStateCfg,
                                              SerializationConfiguration serializationCfg,
                                              int iterations)
        {

            TestDataFutures testDataFutures = ExchangeTestContainer.prepareTestDataAsync(testDataParameters, 1);

            using (ExchangeTestContainer container = ExchangeTestContainer.create(performanceCfg, initialStateCfg, serializationCfg))
            {

                float avgMt = container.executeTestingThread(
                        () => (float)Enumerable.Range(0, iterations)
                                .Select(j =>
                                {
                                    container.loadSymbolsUsersAndPrefillOrdersNoLog(testDataFutures);

                                    float perfMt = container.benchmarkMtps(testDataFutures.GenResult.Result.ApiCommandsBenchmark.Result);
                                    log.Info($"{j}. {String.Format("%.3f", perfMt)} MT/s");

                                    Assert.True(container.totalBalanceReport().isGlobalBalancesAllZero());

                                    // compare orderBook final state just to make sure all commands executed same way
                                    foreach (var symbol in testDataFutures.CoreSymbolSpecifications.Result)
                                    {
                                        Assert.AreEqual(
                                                       testDataFutures.GenResult.Result.GenResults[symbol.SymbolId].FinalOrderBookSnapshot,
                                                       container.requestCurrentOrderBook(symbol.SymbolId));
                                    }

                                    // TODO compare events, balances, positions

                                    container.resetExchangeCore();

                                    GC.Collect();

                                    return perfMt;
                                })
                            .Average());//.orElse(0)) ;

                log.Info($"Average: {avgMt} MT/s");
            }
        }

    }
}

using Exchange.Core.Common;
using Exchange.Core.Common.Api;
using Exchange.Core.Common.Config;
using Exchange.Core.Tests.Utils;
using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Tests.Integration
{
    public abstract class ITExchangeCoreIntegrationStress
    {
        private static ILog log = LogManager.GetLogger(typeof(ITExchangeCoreIntegrationStress));

        // configuration provided by child class
        public abstract PerformanceConfiguration getPerformanceConfiguration();

        [Test, Timeout(60_000)]
        public void manyOperationsMargin()
        {
            manyOperations(TestConstants.SYMBOLSPEC_EUR_USD);
        }

        [Test, Timeout(60_000)]
        public void manyOperationsExchange()
        {
            manyOperations(TestConstants.SYMBOLSPEC_ETH_XBT);
        }

        public void manyOperations(CoreSymbolSpecification symbolSpec)
        {
            using (ExchangeTestContainer container = ExchangeTestContainer.create(getPerformanceConfiguration()))
            {
                container.initBasicSymbols();
                //container.initBasicUsers();
                ExchangeApi api = container.api;

                int numOrders = 1_000_000;
                int targetOrderBookOrders = 1000;
                int numUsers = 1000;

                log.Debug("Generating commands...");
                GenResult genResult = TestOrdersGenerator.generateCommands(
                        numOrders,
                        targetOrderBookOrders,
                        numUsers,
                        TestOrdersGenerator.UID_PLAIN_MAPPER,
                        symbolSpec.SymbolId,
                        false,
                        false,
                        TestOrdersGenerator.createAsyncProgressLogger(numOrders),
                        288379917);

                List<ApiCommand> apiCommands = TestOrdersGenerator.convertToApiCommand(genResult);

                HashSet<int> allowedCurrencies = Enumerable.Range(symbolSpec.QuoteCurrency, symbolSpec.BaseCurrency).ToHashSet();

                log.Debug("Users init ...");
                container.usersInit(numUsers, allowedCurrencies);

                // validate total balance as a sum of loaded funds
                Action<Dictionary<int, long>> balancesValidator = balances =>
                {
                    foreach (var cur in allowedCurrencies)
                        Assert.AreEqual(balances[cur], 10_0000_0000L * numUsers);
                };
                log.Debug("Verifying balances...");
                balancesValidator(container.totalBalanceReport().getClientsBalancesSum());

                log.Debug("Running benchmark...");
                CountdownEvent ordersLatch = new CountdownEvent(apiCommands.Count);
                container.consumer = (cmd, seq) => ordersLatch.Signal();
                foreach (ApiCommand cmd in apiCommands)
                {
                    cmd.Timestamp = DateTime.UtcNow.Ticks;
                    api.submitCommand(cmd);
                }
                ordersLatch.Wait();
                ordersLatch.Reset(); // can't go below zero and orderbook request generate new command

                // compare orderBook final state just to make sure all commands executed same way
                // TODO compare events, wait until finish
                L2MarketData l2MarketData = container.requestCurrentOrderBook(symbolSpec.SymbolId);
                Assert.AreEqual(genResult.FinalOrderBookSnapshot, l2MarketData);
                Assert.IsTrue(l2MarketData.AskSize > 10);
                Assert.IsTrue(l2MarketData.BidSize > 10);

                // verify that total balance was not changed
                balancesValidator(container.totalBalanceReport().getClientsBalancesSum());
            }
        }
    }
}

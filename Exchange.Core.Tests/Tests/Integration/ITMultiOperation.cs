using Exchange.Core.Common.Config;
using Exchange.Core.Tests.Tests.Utils;
using Exchange.Core.Tests.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Tests.Integration
{
    [TestFixture]
    public class ITMultiOperation
    {

        [Test, Timeout(60_000)]
        public void shouldPerformMarginOperations()
        {
            ThroughputTestsModule.throughputTestImpl(
                    PerformanceConfiguration.throughputPerformanceBuilder()
                            .matchingEnginesNum(1)
                            .riskEnginesNum(1)
                            .build(),
                    TestDataParameters.Builder()
                            .totalTransactionsNumber(1_000_000)
                            .targetOrderBookOrdersTotal(1000)
                            .numAccounts(2000)
                            .currenciesAllowed(TestConstants.CURRENCIES_FUTURES)
                            .numSymbols(1)
                            .allowedSymbolTypes(AllowedSymbolTypes.FUTURES_CONTRACT)
                            .preFillMode(PreFillMode.ORDERS_NUMBER)
                            .build(),
                    InitialStateConfiguration.CLEAN_TEST,
                    SerializationConfiguration.DEFAULT,
                    2
            );
        }

        [Test, Timeout(60_000)]
        public void shouldPerformExchangeOperations()
        {
            ThroughputTestsModule.throughputTestImpl(
                    PerformanceConfiguration.throughputPerformanceBuilder()
                            .matchingEnginesNum(1)
                            .riskEnginesNum(1)
                            .build(),
                    TestDataParameters.Builder()
                            .totalTransactionsNumber(1_000_000)
                            .targetOrderBookOrdersTotal(1000)
                            .numAccounts(2000)
                            .currenciesAllowed(TestConstants.CURRENCIES_EXCHANGE)
                            .numSymbols(1)
                            .allowedSymbolTypes(AllowedSymbolTypes.CURRENCY_EXCHANGE_PAIR)
                            .preFillMode(PreFillMode.ORDERS_NUMBER)
                            .build(),
                    InitialStateConfiguration.CLEAN_TEST,
                    SerializationConfiguration.DEFAULT,
                    2);
        }

        [Test, Timeout(60_000)]
        public void shouldPerformSharded()
        {
            ThroughputTestsModule.throughputTestImpl(
                    PerformanceConfiguration.throughputPerformanceBuilder()
                            .matchingEnginesNum(2)
                            .riskEnginesNum(2)
                            .build(),
                    TestDataParameters.Builder()
                            .totalTransactionsNumber(1_000_000)
                            .targetOrderBookOrdersTotal(1000)
                            .numAccounts(2000)
                            .currenciesAllowed(TestConstants.CURRENCIES_EXCHANGE)
                            .numSymbols(32)
                            .allowedSymbolTypes(AllowedSymbolTypes.BOTH)
                            .preFillMode(PreFillMode.ORDERS_NUMBER)
                            .build(),
                    InitialStateConfiguration.CLEAN_TEST,
                    SerializationConfiguration.DEFAULT,
                    2);
        }
    }
}

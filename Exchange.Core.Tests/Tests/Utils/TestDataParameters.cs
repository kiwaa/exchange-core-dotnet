using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Utils
{
    public sealed partial class TestDataParameters
    {
        public static TestDataParametersBuilder singlePairMarginBuilder()
        {
            return Builder()
                    .totalTransactionsNumber(3_000_000)
                    .targetOrderBookOrdersTotal(1000)
                    .numAccounts(2000)
                    .currenciesAllowed(TestConstants.CURRENCIES_FUTURES)
                    .numSymbols(1)
                    .allowedSymbolTypes(AllowedSymbolTypes.FUTURES_CONTRACT)
                    .preFillMode(Utils.PreFillMode.ORDERS_NUMBER);
        }

        public static TestDataParametersBuilder singlePairExchangeBuilder()
        {
            return Builder()
                    .totalTransactionsNumber(3_000_000)
                    .targetOrderBookOrdersTotal(1000)
                    .numAccounts(2000)
                    .currenciesAllowed(TestConstants.CURRENCIES_EXCHANGE)
                    .numSymbols(1)
                    .allowedSymbolTypes(AllowedSymbolTypes.CURRENCY_EXCHANGE_PAIR)
                    .preFillMode(Utils.PreFillMode.ORDERS_NUMBER);
        }

        /**
         * - 1M active users (3M currency accounts)
         * - 1M pending limit-orders
         * - 10K symbols
         *
         * @return medium exchange test data configuration
         */
        public static TestDataParametersBuilder mediumBuilder()
        {
            return Builder()
                    .totalTransactionsNumber(3_000_000)
                    .targetOrderBookOrdersTotal(1_000_000)
                    .numAccounts(3_300_000)
                    .currenciesAllowed(TestConstants.ALL_CURRENCIES)
                    .numSymbols(10_000)
                    .allowedSymbolTypes(AllowedSymbolTypes.BOTH)
                    .preFillMode(Utils.PreFillMode.ORDERS_NUMBER_PLUS_QUARTER);
        }

        /**
         * - 3M active users (10M currency accounts)
         * - 3M pending limit-orders
         * - 50K symbols
         *
         * @return large exchange test data configuration
         */
        public static TestDataParametersBuilder largeBuilder()
        {
            return Builder()
                    .totalTransactionsNumber(3_000_000)
                    .targetOrderBookOrdersTotal(3_000_000)
                    .numAccounts(10_000_000)
                    .currenciesAllowed(TestConstants.ALL_CURRENCIES)
                    .numSymbols(50_000)
                    .allowedSymbolTypes(AllowedSymbolTypes.BOTH)
                    .preFillMode(Utils.PreFillMode.ORDERS_NUMBER);
        }

        /**
         * - 10M active users (33M currency accounts)
         * - 30M pending limit-orders
         * - 100K symbols
         *
         * @return huge exchange test data configuration
         */
        public static TestDataParametersBuilder hugeBuilder()
        {
            return Builder()
                    .totalTransactionsNumber(10_000_000)
                    .targetOrderBookOrdersTotal(30_000_000)
                    .numAccounts(33_000_000)
                    .currenciesAllowed(TestConstants.ALL_CURRENCIES)
                    .numSymbols(100_000)
                    .allowedSymbolTypes(AllowedSymbolTypes.BOTH)
                    .preFillMode(Utils.PreFillMode.ORDERS_NUMBER);
        }
    }
}

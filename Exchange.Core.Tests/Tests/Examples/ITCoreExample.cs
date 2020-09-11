using Exchange.Core.Common;
using Exchange.Core.Common.Api;
using Exchange.Core.Common.Api.Binary;
using Exchange.Core.Common.Api.Reports;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Common.Config;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Examples
{
    [TestFixture]
    public class ITCoreExample
    {
        [Test]
        public void SampleTest()
        {
            // simple async events handler
            SimpleEventsProcessor eventsProcessor = new SimpleEventsProcessor(new EventsHandlerImpl());

            // default exchange configuration
            ExchangeConfiguration conf = ExchangeConfiguration.defaultBuilder().build();

            // build exchange core
            ExchangeCore exchangeCore = ExchangeCore.Builder()
                    .resultsConsumer(eventsProcessor.accept)
                    .exchangeConfiguration(conf)
                    .build();

            // start up disruptor threads
            exchangeCore.startup();

            // get exchange API for publishing commands
            ExchangeApi api = exchangeCore.getApi();

            // currency code constants
            const int currencyCodeXbt = 11;
            const int currencyCodeLtc = 15;

            // symbol constants
            const int symbolXbtLtc = 241;

            Task<CommandResultCode> future;

            // create symbol specification and publish it
            CoreSymbolSpecification symbolSpecXbtLtc = CoreSymbolSpecification.Builder()
                .symbolId(symbolXbtLtc)         // symbol id
                .type(SymbolType.CURRENCY_EXCHANGE_PAIR)
                .baseCurrency(currencyCodeXbt)    // base = satoshi (1E-8)
                .quoteCurrency(currencyCodeLtc)   // quote = litoshi (1E-8)
                .baseScaleK(1_000_000L) // 1 lot = 1M satoshi (0.01 BTC)
                .quoteScaleK(10_000L)   // 1 price step = 10K litoshi
                .takerFee(1900L)        // taker fee 1900 litoshi per 1 lot
                .makerFee(700L)         // maker fee 700 litoshi per 1 lot
                .build();

            future = api.submitBinaryDataAsync(new BatchAddSymbolsCommand(symbolSpecXbtLtc));
            Console.WriteLine("BatchAddSymbolsCommand result: " + future.Result);


            // create user uid=301
            future = api.submitCommandAsync(ApiAddUser.Builder()
                    .uid(301L)
                    .build());

            Console.WriteLine("ApiAddUser 1 result: " + future.Result);

            // create user uid=302
            future = api.submitCommandAsync(ApiAddUser.Builder()
                    .uid(302L)
                    .build());

            Console.WriteLine("ApiAddUser 2 result: " + future.Result);

            // first user deposits 20 LTC
            future = api.submitCommandAsync(ApiAdjustUserBalance.Builder()
                    .uid(301L)
                    .currency(currencyCodeLtc)
                    .amount(2_000_000_000L)
                    .transactionId(1L)
                    .build());

            Console.WriteLine("ApiAdjustUserBalance 1 result: " + future.Result);


            // second user deposits 0.10 BTC
            future = api.submitCommandAsync(ApiAdjustUserBalance.Builder()
                    .uid(302L)
                    .currency(currencyCodeXbt)
                    .amount(10_000_000L)
                    .transactionId(2L)
                    .build());

            Console.WriteLine("ApiAdjustUserBalance 2 result: " + future.Result);


            // first user places Good-till-Cancel Bid order
            // he assumes BTCLTC exchange rate 154 LTC for 1 BTC
            // bid price for 1 lot (0.01BTC) is 1.54 LTC => 1_5400_0000 litoshi => 10K * 15_400 (in price steps)
            future = api.submitCommandAsync(ApiPlaceOrder.Builder()
                    .uid(301L)
                    .orderId(5001L)
                    .price(15_400L)
                    .reservePrice(15_600L) // can move bid order up to the 1.56 LTC, without replacing it
                    .size(12L) // order size is 12 lots
                    .action(OrderAction.BID)
                    .orderType(OrderType.GTC) // Good-till-Cancel
                    .symbol(symbolXbtLtc)
                    .build());

            Console.WriteLine("ApiPlaceOrder 1 result: " + future.Result);


            // second user places Immediate-or-Cancel Ask (Sell) order
            // he assumes wost rate to sell 152.5 LTC for 1 BTC
            future = api.submitCommandAsync(ApiPlaceOrder.Builder()
                    .uid(302L)
                    .orderId(5002L)
                    .price(15_250L)
                    .size(10L) // order size is 10 lots
                    .action(OrderAction.ASK)
                    .orderType(OrderType.IOC) // Immediate-or-Cancel
                    .symbol(symbolXbtLtc)
                    .build());

            Console.WriteLine("ApiPlaceOrder 2 result: " + future.Result);


            // request order book
            //Task<L2MarketData> orderBookFuture = api.requestOrderBookAsync(symbolXbtLtc, 10);
            Task<L2MarketData> orderBookFuture = api.requestOrderBookAsync(symbolXbtLtc, 10);
            Console.WriteLine("ApiOrderBookRequest result: " + orderBookFuture.Result);


            // first user moves remaining order to price 1.53 LTC
            future = api.submitCommandAsync(ApiMoveOrder.Builder()
                    .uid(301L)
                    .orderId(5001L)
                    .newPrice(15_300L)
                    .symbol(symbolXbtLtc)
                    .build());

            Console.WriteLine("ApiMoveOrder 2 result: " + future.Result);

            // first user cancel remaining order
            future = api.submitCommandAsync(ApiCancelOrder.Builder()
                    .uid(301L)
                    .orderId(5001L)
                    .symbol(symbolXbtLtc)
                    .build());

            Console.WriteLine("ApiCancelOrder 2 result: " + future.Result);

            // check balances
            Task<SingleUserReportResult> report1 = api.processReport<SingleUserReportResult>(new SingleUserReportQuery(301), 0);
            Console.WriteLine("SingleUserReportQuery 1 accounts: " + report1.Result.Accounts);

            Task<SingleUserReportResult> report2 = api.processReport<SingleUserReportResult>(new SingleUserReportQuery(302), 0);
            Console.WriteLine("SingleUserReportQuery 2 accounts: " + report2.Result.Accounts);

            // first user withdraws 0.10 BTC
            future = api.submitCommandAsync(ApiAdjustUserBalance.Builder()
                    .uid(301L)
                    .currency(currencyCodeXbt)
                    .amount(-10_000_000L)
                    .transactionId(3L)
                    .build());

            Console.WriteLine("ApiAdjustUserBalance 1 result: " + future.Result);

            // check fees collected
            Task<TotalCurrencyBalanceReportResult> totalsReport = api.processReport<TotalCurrencyBalanceReportResult>(new TotalCurrencyBalanceReportQuery(), 0);
            Console.WriteLine("LTC fees collected: " + totalsReport.Result.Fees[currencyCodeLtc]);

        }

        class EventsHandlerImpl : IEventsHandler
        {
            public void tradeEvent(TradeEvent tradeEvent)
            {
                Console.WriteLine("Trade event: " + tradeEvent);
            }


            public void reduceEvent(ReduceEvent reduceEvent)
            {
                Console.WriteLine("Reduce event: " + reduceEvent);
            }


            public void rejectEvent(RejectEvent rejectEvent)
            {
                Console.WriteLine("Reject event: " + rejectEvent);
            }


            public void commandResult(ApiCommandResult commandResult)
            {
                Console.WriteLine("Command result: " + commandResult);
            }


            public void orderBook(OrderBook orderBook)
            {
                Console.WriteLine("OrderBook event: " + orderBook);
            }
        }
    }
}

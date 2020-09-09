using Exchange.Core.Common;
using Exchange.Core.Common.Api;
using Exchange.Core.Common.Api.Reports;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Common.Config;
using Exchange.Core.Tests.Utils;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exchange.Core.Tests.Tests.Integration
{
    public abstract class ITFeesExchange
    {
        private readonly long step = TestConstants.SYMBOLSPECFEE_XBT_LTC.QuoteScaleK;
        private readonly long makerFee = TestConstants.SYMBOLSPECFEE_XBT_LTC.MakerFee;
        private readonly long takerFee = TestConstants.SYMBOLSPECFEE_XBT_LTC.TakerFee;

        // configuration provided by child class
        public abstract PerformanceConfiguration getPerformanceConfiguration();


        [Test, Timeout(10_000)]
        public void shouldRequireTakerFees_GtcCancel()
        {

            using (ExchangeTestContainer container = ExchangeTestContainer.create(getPerformanceConfiguration()))
            {
                container.initFeeSymbols();

                // ----------------- 1 test GTC BID cancel ------------------

                // create user - 3.42B litoshi (34.2 LTC)
                long ltcAmount = 3_420_000_000L;
                container.createUserWithMoney(TestConstants.UID_2, TestConstants.CURRENECY_LTC, ltcAmount);

                // submit BID order for 1000 lots - should be rejected because of the fee
                ApiPlaceOrder order203 = ApiPlaceOrder.Builder().uid(TestConstants.UID_2).orderId(203).price(11_400).reservePrice(11_400).size(30).action(OrderAction.BID).orderType(OrderType.GTC).symbol(TestConstants.SYMBOL_EXCHANGE_FEE).build();
                container.submitCommandSync(order203, CommandResultCode.RISK_NSF);

                // add fee-1 - NSF
                container.addMoneyToUser(TestConstants.UID_2, TestConstants.CURRENECY_LTC, takerFee * 30 - 1);
                container.submitCommandSync(order203, CommandResultCode.RISK_NSF);

                // add 1 extra - SUCCESS
                container.addMoneyToUser(TestConstants.UID_2, TestConstants.CURRENECY_LTC, 1);
                container.submitCommandSync(order203, CommandResultCode.SUCCESS);

                // cancel bid
                container.submitCommandSync(
                        ApiCancelOrder.Builder().orderId(203).uid(TestConstants.UID_2).symbol(TestConstants.SYMBOL_EXCHANGE_FEE).build(),
                        CommandResultCode.SUCCESS);

                container.validateUserState(TestConstants.UID_2, profile =>
                {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_LTC], ltcAmount + takerFee * 30);
                    Assert.AreEqual(profile.fetchIndexedOrders().Count == 0, true);
                });

                TotalCurrencyBalanceReportResult totalBal1 = container.totalBalanceReport();
                Assert.AreEqual(totalBal1.isGlobalBalancesAllZero(), true);
                Assert.AreEqual(totalBal1.getClientsBalancesSum()[TestConstants.CURRENECY_LTC], ltcAmount + takerFee * 30);
                totalBal1.Fees.TryGetValue(TestConstants.CURRENECY_LTC, out long fee);
                Assert.AreEqual(fee, 0L);

                // ----------------- 2 test GTC ASK cancel ------------------

                // add 100M satoshi (1 BTC)
                long btcAmount = 100_000_000L;
                container.addMoneyToUser(TestConstants.UID_2, TestConstants.CURRENECY_XBT, btcAmount);

                // can place ASK order, no extra is fee required for lock hold
                ApiPlaceOrder order204 = ApiPlaceOrder.Builder().uid(TestConstants.UID_2).orderId(204).price(11_400).reservePrice(11_400).size(100).action(OrderAction.ASK).orderType(OrderType.GTC).symbol(TestConstants.SYMBOL_EXCHANGE_FEE).build();
                container.submitCommandSync(order204, CommandResultCode.SUCCESS);

                // cancel ask
                container.submitCommandSync(
                        ApiCancelOrder.Builder().orderId(204).uid(TestConstants.UID_2).symbol(TestConstants.SYMBOL_EXCHANGE_FEE).build(),
                        CommandResultCode.SUCCESS);

                container.validateUserState(TestConstants.UID_2, profile =>
                {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_XBT], btcAmount);
                    Assert.AreEqual(profile.fetchIndexedOrders().Count == 0, true);
                });

                // no fees collected
                TotalCurrencyBalanceReportResult totalBal2 = container.totalBalanceReport();
                Assert.AreEqual(totalBal2.isGlobalBalancesAllZero(), true);
                Assert.AreEqual(totalBal2.getClientsBalancesSum()[TestConstants.CURRENECY_LTC], ltcAmount + takerFee * 30);
                Assert.AreEqual(totalBal2.getClientsBalancesSum()[TestConstants.CURRENECY_XBT], btcAmount);
                totalBal2.Fees.TryGetValue(TestConstants.CURRENECY_LTC, out fee);
                Assert.AreEqual(fee, 0L);
                totalBal2.Fees.TryGetValue(TestConstants.CURRENECY_XBT, out fee);
                Assert.AreEqual(fee, 0L);
            }
        }


        [Test, Timeout(10_000)]
        public void shouldProcessFees_BidGtcMaker_AskIocTakerPartial()
        {

            using (ExchangeTestContainer container = ExchangeTestContainer.create(getPerformanceConfiguration()))
            {
                container.initFeeSymbols();
                long ltcAmount = 200_000_000_000L;
                container.createUserWithMoney(TestConstants.UID_1, TestConstants.CURRENECY_LTC, ltcAmount); // 200B litoshi (2,000 LTC)

                // submit an GtC order - limit BUY 1,731 lots, price 115M (11,500 x10,000 step) for each lot 1M satoshi
                ApiPlaceOrder order101 = ApiPlaceOrder.Builder()
                        .uid(TestConstants.UID_1)
                        .orderId(101L)
                        .price(11_500L)
                        .reservePrice(11_553L)
                        .size(1731L)
                        .action(OrderAction.BID)
                        .orderType(OrderType.GTC)
                        .symbol(TestConstants.SYMBOL_EXCHANGE_FEE)
                        .build();

                container.submitCommandSync(order101, cmd => Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS));

                long expectedFundsLtc = ltcAmount - (order101.ReservePrice * step + takerFee) * order101.Size;
                // verify order placed with correct reserve price and account balance is updated accordingly
                container.validateUserState(TestConstants.UID_1, profile =>
                {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_LTC], expectedFundsLtc);
                    Assert.AreEqual(profile.fetchIndexedOrders()[101L].Price, order101.Price);
                });

                // create second user
                long btcAmount = 2_000_000_000L;
                container.createUserWithMoney(TestConstants.UID_2, TestConstants.CURRENECY_XBT, btcAmount);

                // no fees collected
                TotalCurrencyBalanceReportResult totalBal1 = container.totalBalanceReport();
                Assert.AreEqual(totalBal1.isGlobalBalancesAllZero(), true);
                Assert.AreEqual(totalBal1.getClientsBalancesSum()[TestConstants.CURRENECY_LTC], ltcAmount);
                Assert.AreEqual(totalBal1.getClientsBalancesSum()[TestConstants.CURRENECY_XBT], btcAmount);
                totalBal1.Fees.TryGetValue(TestConstants.CURRENECY_LTC, out long fee);
                Assert.AreEqual(fee, 0L);

                // submit an IoC order - sell 2,000 lots, price 114,930K (11,493 x10,000 step)
                ApiPlaceOrder order102 = ApiPlaceOrder.Builder()
                        .uid(TestConstants.UID_2)
                        .orderId(102)
                        .price(11_493L)
                        .size(2000L)
                        .action(OrderAction.ASK)
                        .orderType(OrderType.IOC)
                        .symbol(TestConstants.SYMBOL_EXCHANGE_FEE)
                        .build();

                container.submitCommandSync(order102, cmd => Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS));

                // verify buyer maker balance
                container.validateUserState(TestConstants.UID_1, profile =>
                {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_LTC], ltcAmount - (order101.Price * step + makerFee) * 1731L);
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_XBT], 1731L * TestConstants.SYMBOLSPECFEE_XBT_LTC.BaseScaleK);
                    Assert.AreEqual(profile.fetchIndexedOrders().Count == 0, true);
                });

                // verify seller taker balance
                container.validateUserState(TestConstants.UID_2, profile =>
                {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_LTC], (order101.Price * step - takerFee) * 1731L);
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_XBT], btcAmount - 1731L * TestConstants.SYMBOLSPECFEE_XBT_LTC.BaseScaleK);
                    Assert.AreEqual(profile.fetchIndexedOrders().Count == 0, true);
                });

                // total balance remains the same
                TotalCurrencyBalanceReportResult totalBal2 = container.totalBalanceReport();
                long ltcFees = (makerFee + takerFee) * 1731L;
                Assert.AreEqual(totalBal2.isGlobalBalancesAllZero(), true);
                Assert.AreEqual(totalBal2.Fees[TestConstants.CURRENECY_LTC], ltcFees);
                Assert.AreEqual(totalBal2.getClientsBalancesSum()[TestConstants.CURRENECY_LTC], ltcAmount - ltcFees);
                Assert.AreEqual(totalBal2.getClientsBalancesSum()[TestConstants.CURRENECY_XBT], btcAmount);
            }

        }


        [Test, Timeout(10_000)]
        public void shouldProcessFees_BidGtcMakerPartial_AskIocTaker()
        {

            using (ExchangeTestContainer container = ExchangeTestContainer.create(getPerformanceConfiguration()))
            {
                container.initFeeSymbols();
                long ltcAmount = 200_000_000_000L;
                container.createUserWithMoney(TestConstants.UID_1, TestConstants.CURRENECY_LTC, ltcAmount); // 200B litoshi (2,000 LTC)

                // submit an GtC order - limit BUY 1,731 lots, price 115M (11,500 x10,000 step) for each lot 1M satoshi
                ApiPlaceOrder order101 = ApiPlaceOrder.Builder()
                        .uid(TestConstants.UID_1)
                        .orderId(101L)
                        .price(11_500L)
                        .reservePrice(11_553L)
                        .size(1731L)
                        .action(OrderAction.BID)
                        .orderType(OrderType.GTC)
                        .symbol(TestConstants.SYMBOL_EXCHANGE_FEE)
                        .build();

                container.submitCommandSync(order101, cmd => Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS));

                long expectedFundsLtc = ltcAmount - (order101.ReservePrice * step + takerFee) * order101.Size;
                // verify order placed with correct reserve price and account balance is updated accordingly
                container.validateUserState(TestConstants.UID_1, profile =>
                {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_LTC], expectedFundsLtc);
                    Assert.AreEqual(profile.fetchIndexedOrders()[101L].Price, order101.Price);
                });

                // create second user
                long btcAmount = 2_000_000_000L;
                container.createUserWithMoney(TestConstants.UID_2, TestConstants.CURRENECY_XBT, btcAmount);

                // no fees collected
                TotalCurrencyBalanceReportResult totalBal1 = container.totalBalanceReport();
                Assert.AreEqual(totalBal1.isGlobalBalancesAllZero(), true);
                Assert.AreEqual(totalBal1.getClientsBalancesSum()[TestConstants.CURRENECY_LTC], ltcAmount);
                Assert.AreEqual(totalBal1.getClientsBalancesSum()[TestConstants.CURRENECY_XBT], btcAmount);
                totalBal1.Fees.TryGetValue(TestConstants.CURRENECY_LTC, out long fee);
                Assert.AreEqual(fee, 0L);

                // submit an IoC order - sell 1,000 lots, price 114,930K (11,493 x10,000 step)
                ApiPlaceOrder order102 = ApiPlaceOrder.Builder()
                        .uid(TestConstants.UID_2)
                        .orderId(102)
                        .price(11_493L)
                        .size(1000L)
                        .action(OrderAction.ASK)
                        .orderType(OrderType.IOC)
                        .symbol(TestConstants.SYMBOL_EXCHANGE_FEE)
                        .build();

                container.submitCommandSync(order102, cmd => Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS));

                // verify buyer maker balance
                container.validateUserState(TestConstants.UID_1, profile =>
                {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_LTC],
                            ltcAmount - (order101.Price * step + makerFee) * 1000L - (order101.ReservePrice * step + takerFee) * 731L);
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_XBT], 1000L * TestConstants.SYMBOLSPECFEE_XBT_LTC.BaseScaleK);
                    Assert.AreEqual(profile.fetchIndexedOrders().Count == 0, false);
                });

                // verify seller taker balance
                container.validateUserState(TestConstants.UID_2, profile =>
                {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_LTC], (order101.Price * step - takerFee) * 1000L);
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_XBT], btcAmount - 1000L * TestConstants.SYMBOLSPECFEE_XBT_LTC.BaseScaleK);
                    Assert.AreEqual(profile.fetchIndexedOrders().Count == 0, true);
                });

                // total balance remains the same
                TotalCurrencyBalanceReportResult totalBal2 = container.totalBalanceReport();
                Assert.AreEqual(totalBal2.isGlobalBalancesAllZero(), true);
                long ltcFees = (makerFee + takerFee) * 1000L;
                Assert.AreEqual(totalBal2.Fees[TestConstants.CURRENECY_LTC], ltcFees);
                Assert.AreEqual(totalBal2.getClientsBalancesSum()[TestConstants.CURRENECY_LTC], ltcAmount - ltcFees);
                Assert.AreEqual(totalBal2.getClientsBalancesSum()[TestConstants.CURRENECY_XBT], btcAmount);
            }

        }
        [Test, Timeout(10_000)]
        public void shouldProcessFees_AskGtcMaker_BidIocTakerPartial()
        {

            using (ExchangeTestContainer container = ExchangeTestContainer.create(getPerformanceConfiguration()))
            {
                container.initFeeSymbols();

                long btcAmount = 2_000_000_000L;
                container.createUserWithMoney(TestConstants.UID_1, TestConstants.CURRENECY_XBT, btcAmount);

                // submit an ASK GtC order, no fees, sell 2,000 lots, price 115,000K (11,500 x10,000 step)
                ApiPlaceOrder order101 = ApiPlaceOrder.Builder()
                        .uid(TestConstants.UID_1)
                        .orderId(101L)
                        .price(11_500L)
                        .reservePrice(11_500L)
                        .size(2000L)
                        .action(OrderAction.ASK)
                        .orderType(OrderType.GTC)
                        .symbol(TestConstants.SYMBOL_EXCHANGE_FEE)
                        .build();

                container.submitCommandSync(order101, cmd=>Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS));

                // verify order placed
                container.validateUserState(TestConstants.UID_1, profile=> {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_XBT], 0L);
                    Assert.AreEqual(profile.fetchIndexedOrders()[101L].Price, order101.Price);
                });

                // create second user
                long ltcAmount = 260_000_000_000L;// 260B litoshi (2,600 LTC)
                container.createUserWithMoney(TestConstants.UID_2, TestConstants.CURRENECY_LTC, ltcAmount);

                TotalCurrencyBalanceReportResult totalBal1 = container.totalBalanceReport();
                Assert.AreEqual(totalBal1.isGlobalBalancesAllZero(), true);
                Assert.AreEqual(totalBal1.getClientsBalancesSum()[TestConstants.CURRENECY_LTC], ltcAmount);
                Assert.AreEqual(totalBal1.getClientsBalancesSum()[TestConstants.CURRENECY_XBT], btcAmount);
                totalBal1.Fees.TryGetValue(TestConstants.CURRENECY_LTC, out long fee);
                Assert.AreEqual(fee, 0L);

                // submit an IoC order - ASK 2,197 lots, price 115,210K (11,521 x10,000 step) for each lot 1M satoshi
                ApiPlaceOrder order102 = ApiPlaceOrder.Builder()
                        .uid(TestConstants.UID_2)
                        .orderId(102)
                        .price(11_521L)
                        .reservePrice(11_659L)
                        .size(2197L)
                        .action(OrderAction.BID)
                        .orderType(OrderType.IOC)
                        .symbol(TestConstants.SYMBOL_EXCHANGE_FEE)
                        .build();

                container.submitCommandSync(order102, cmd=>Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS));

                // verify seller maker balance
                container.validateUserState(TestConstants.UID_1, profile=> {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_XBT], 0L);
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_LTC], (11_500L * step - makerFee) * 2000L);
                    Assert.AreEqual(profile.fetchIndexedOrders().Count == 0, true);
                });

                // verify buyer taker balance
                container.validateUserState(TestConstants.UID_2, profile=> {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_XBT], TestConstants.SYMBOLSPECFEE_XBT_LTC.BaseScaleK * 2000L);
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_LTC], ltcAmount - (11_500L * step + takerFee) * 2000L);
                    Assert.AreEqual(profile.fetchIndexedOrders().Count == 0, true);
                });

                // total balance remains the same
                TotalCurrencyBalanceReportResult totalBal2 = container.totalBalanceReport();
                long ltcFees = (makerFee + takerFee) * 2000L;
                Assert.AreEqual(totalBal2.isGlobalBalancesAllZero(), true);
                Assert.AreEqual(totalBal2.Fees[TestConstants.CURRENECY_LTC], ltcFees);
                Assert.AreEqual(totalBal2.getClientsBalancesSum()[TestConstants.CURRENECY_LTC], ltcAmount - ltcFees);
                Assert.AreEqual(totalBal2.getClientsBalancesSum()[TestConstants.CURRENECY_XBT], btcAmount);
            }

        }
        [Test, Timeout(10_000)]
        public void shouldProcessFees_AskGtcMakerPartial_BidIocTaker()
        {

            using (ExchangeTestContainer container = ExchangeTestContainer.create(getPerformanceConfiguration()))
            {
                container.initFeeSymbols();

                long btcAmount = 2_000_000_000L;
                container.createUserWithMoney(TestConstants.UID_1, TestConstants.CURRENECY_XBT, btcAmount);

                // submit an ASK GtC order, no fees, sell 2,000 lots, price 115,000K (11,500 x10,000 step)
                ApiPlaceOrder order101 = ApiPlaceOrder.Builder()
                        .uid(TestConstants.UID_1)
                        .orderId(101L)
                        .price(11_500L)
                        .reservePrice(11_500L)
                        .size(2000L)
                        .action(OrderAction.ASK)
                        .orderType(OrderType.GTC)
                        .symbol(TestConstants.SYMBOL_EXCHANGE_FEE)
                        .build();

                container.submitCommandSync(order101, cmd=>Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS));

                // verify order placed
                container.validateUserState(TestConstants.UID_1, profile=> {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_XBT], 0L);
                    Assert.AreEqual(profile.fetchIndexedOrders()[101L].Price, order101.Price);
                });

                // create second user
                long ltcAmount = 260_000_000_000L;// 260B litoshi (2,600 LTC)
                container.createUserWithMoney(TestConstants.UID_2, TestConstants.CURRENECY_LTC, ltcAmount);

                TotalCurrencyBalanceReportResult totalBal1 = container.totalBalanceReport();
                totalBal1.Fees.TryGetValue(TestConstants.CURRENECY_LTC, out long fee);
                Assert.AreEqual(fee, 0L);
                Assert.AreEqual(totalBal1.isGlobalBalancesAllZero(), true);
                Assert.AreEqual(totalBal1.getClientsBalancesSum()[TestConstants.CURRENECY_LTC], ltcAmount);
                Assert.AreEqual(totalBal1.getClientsBalancesSum()[TestConstants.CURRENECY_XBT], btcAmount);

                // submit an IoC order - ASK 1,997 lots, price 115,210K (11,521 x10,000 step) for each lot 1M satoshi
                ApiPlaceOrder order102 = ApiPlaceOrder.Builder()
                        .uid(TestConstants.UID_2)
                        .orderId(102)
                        .price(11_521L)
                        .reservePrice(11_659L)
                        .size(1997L)
                        .action(OrderAction.BID)
                        .orderType(OrderType.IOC)
                        .symbol(TestConstants.SYMBOL_EXCHANGE_FEE)
                        .build();

                container.submitCommandSync(order102, cmd=>Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS));

                // verify seller maker balance
                container.validateUserState(TestConstants.UID_1, profile=> {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_XBT], 0L);
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_LTC], (11_500L * step - makerFee) * 1997L);
                    Assert.AreEqual(profile.fetchIndexedOrders().Count == 0, false);
                });

                // verify buyer taker balance
                container.validateUserState(TestConstants.UID_2, profile=> {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_XBT], TestConstants.SYMBOLSPECFEE_XBT_LTC.BaseScaleK * 1997L);
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_LTC], ltcAmount - (11_500L * step + takerFee) * 1997L);
                    Assert.AreEqual(profile.fetchIndexedOrders().Count == 0, true);
                });

                // total balance remains the same
                long ltcFees = (makerFee + takerFee) * 1997L;
                TotalCurrencyBalanceReportResult totalBal2 = container.totalBalanceReport();
                Assert.AreEqual(totalBal2.isGlobalBalancesAllZero(), true);
                Assert.AreEqual(totalBal2.getClientsBalancesSum()[TestConstants.CURRENECY_LTC], ltcAmount - ltcFees);
                Assert.AreEqual(totalBal2.getClientsBalancesSum()[TestConstants.CURRENECY_XBT], btcAmount);
                Assert.AreEqual(totalBal2.Fees[TestConstants.CURRENECY_LTC], ltcFees);
            }
        }

    }
}

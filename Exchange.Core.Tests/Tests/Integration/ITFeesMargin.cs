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
    public abstract class ITFeesMargin
    {
        private readonly long makerFee = TestConstants.SYMBOLSPECFEE_USD_JPY.MakerFee;
        private readonly long takerFee = TestConstants.SYMBOLSPECFEE_USD_JPY.TakerFee;
        private readonly int symbolId = TestConstants.SYMBOLSPECFEE_USD_JPY.SymbolId;

        // configuration provided by child class
        public abstract PerformanceConfiguration getPerformanceConfiguration();

        [Test, Timeout(10_000)]
        public void shouldProcessFees_AskGtcMakerPartial_BidIocTaker()
        {

            using (ExchangeTestContainer container = ExchangeTestContainer.create(getPerformanceConfiguration()))
            {
                container.addSymbol(TestConstants.SYMBOLSPECFEE_USD_JPY);

                long jpyAmount1 = 240_000L;
                container.createUserWithMoney(TestConstants.UID_1, TestConstants.CURRENECY_JPY, jpyAmount1);

                ApiPlaceOrder order101 = ApiPlaceOrder.Builder()
                        .uid(TestConstants.UID_1)
                        .orderId(101L)
                        .price(10770L)
                        .reservePrice(0L)
                        .size(40L)
                        .action(OrderAction.ASK)
                        .orderType(OrderType.GTC)
                        .symbol(symbolId)
                        .build();

                container.submitCommandSync(order101, cmd=>Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS));

                // verify order placed
                container.validateUserState(TestConstants.UID_1, profile=> {
                    profile.Accounts.TryGetValue(TestConstants.CURRENECY_XBT, out long accountValue);
                    Assert.AreEqual(accountValue, 0L);
                    Assert.AreEqual(profile.fetchIndexedOrders()[101L].Price, order101.Price);
                });

                // create second user
                long jpyAmount2 = 150_000L;
                container.createUserWithMoney(TestConstants.UID_2, TestConstants.CURRENECY_JPY, jpyAmount2);

                TotalCurrencyBalanceReportResult totalBal1 = container.totalBalanceReport();
                totalBal1.getClientsBalancesSum().TryGetValue(TestConstants.CURRENECY_USD, out long fee);
                Assert.AreEqual(fee, 0L);
                Assert.AreEqual(totalBal1.getClientsBalancesSum()[TestConstants.CURRENECY_JPY], jpyAmount1 + jpyAmount2);
                totalBal1.Fees.TryGetValue(TestConstants.CURRENECY_USD, out fee);
                Assert.AreEqual(fee, 0L);
                totalBal1.Fees.TryGetValue(TestConstants.CURRENECY_JPY, out fee);
                Assert.AreEqual(fee, 0L);
                totalBal1.OpenInterestLong.TryGetValue(symbolId, out long openInterest);
                Assert.AreEqual(openInterest, 0L);

                ApiPlaceOrder order102 = ApiPlaceOrder.Builder()
                        .uid(TestConstants.UID_2)
                        .orderId(102)
                        .price(10770L)
                        .reservePrice(10770L)
                        .size(30L)
                        .action(OrderAction.BID)
                        .orderType(OrderType.IOC)
                        .symbol(symbolId)
                        .build();

                container.submitCommandSync(order102, cmd=>Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS));

                // verify seller maker balance
                container.validateUserState(TestConstants.UID_1, profile=> {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_JPY], 240_000L - makerFee * 30);
                    profile.Accounts.TryGetValue(TestConstants.CURRENECY_USD, out long accountValue);
                    Assert.AreEqual(accountValue, 0L);
                    Assert.AreEqual(profile.Positions[symbolId].direction, PositionDirection.SHORT);
                    Assert.AreEqual(profile.Positions[symbolId].openVolume, 30L);
                    Assert.AreEqual(profile.Positions[symbolId].pendingBuySize, 0L);
                    Assert.AreEqual(profile.Positions[symbolId].pendingSellSize, 10L);
                    Assert.False(profile.fetchIndexedOrders().Count == 0);
                });

                // verify buyer taker balance
                container.validateUserState(TestConstants.UID_2, profile=> {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_JPY], 150_000L - takerFee * 30);
                    profile.Accounts.TryGetValue(TestConstants.CURRENECY_USD, out long accountValue);
                    Assert.AreEqual(accountValue, 0L);
                    Assert.AreEqual(profile.Positions[symbolId].direction, PositionDirection.LONG);
                    Assert.AreEqual(profile.Positions[symbolId].openVolume, 30L);
                    Assert.AreEqual(profile.Positions[symbolId].pendingBuySize, 0L);
                    Assert.AreEqual(profile.Positions[symbolId].pendingSellSize, 0L);
                    Assert.True(profile.fetchIndexedOrders().Count == 0);
                });

                // total balance remains the same
                TotalCurrencyBalanceReportResult totalBal2 = container.totalBalanceReport();
                long jpyFees = (makerFee + takerFee) * 30;
                totalBal2.Fees.TryGetValue(TestConstants.CURRENECY_USD, out fee);
                Assert.AreEqual(fee, 0L);
                Assert.AreEqual(totalBal2.Fees[TestConstants.CURRENECY_JPY], jpyFees);
                totalBal2.getClientsBalancesSum().TryGetValue(TestConstants.CURRENECY_USD, out long accountValue);
                Assert.AreEqual(accountValue, 0L);
                Assert.AreEqual(totalBal2.getClientsBalancesSum()[TestConstants.CURRENECY_JPY], jpyAmount1 + jpyAmount2 - jpyFees);
                Assert.AreEqual(totalBal2.OpenInterestLong[symbolId], 30L);
            }
        }

        [Test, Timeout(10_000)]
        public void shouldProcessFees_BidGtcMakerPartial_AskIocTaker()
        {

            using (ExchangeTestContainer container = ExchangeTestContainer.create(getPerformanceConfiguration()))
            {
                container.addSymbol(TestConstants.SYMBOLSPECFEE_USD_JPY);

                long jpyAmount1 = 250_000L;
                container.createUserWithMoney(TestConstants.UID_1, TestConstants.CURRENECY_JPY, jpyAmount1);

                ApiPlaceOrder order101 = ApiPlaceOrder.Builder()
                        .uid(TestConstants.UID_1)
                        .orderId(101L)
                        .price(10770L)
                        .reservePrice(0L)
                        .size(50L)
                        .action(OrderAction.BID)
                        .orderType(OrderType.GTC)
                        .symbol(symbolId)
                        .build();

                container.submitCommandSync(order101, cmd => Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS));

                // verify order placed
                container.validateUserState(TestConstants.UID_1, profile=> {
                    profile.Accounts.TryGetValue(TestConstants.CURRENECY_XBT, out long accountValue);
                    Assert.AreEqual(accountValue, 0L);
                    Assert.AreEqual(profile.fetchIndexedOrders()[101L].Price, order101.Price);
                });

                // create second user
                long jpyAmount2 = 200_000L;
                container.createUserWithMoney(TestConstants.UID_2, TestConstants.CURRENECY_JPY, jpyAmount2);

                TotalCurrencyBalanceReportResult totalBal1 = container.totalBalanceReport();
                totalBal1.getClientsBalancesSum().TryGetValue(TestConstants.CURRENECY_USD, out long fee);
                Assert.AreEqual(fee, 0L);
                Assert.AreEqual(totalBal1.getClientsBalancesSum()[TestConstants.CURRENECY_JPY], jpyAmount1 + jpyAmount2);
                totalBal1.Fees.TryGetValue(TestConstants.CURRENECY_USD, out fee);
                Assert.AreEqual(fee, 0L);
                totalBal1.Fees.TryGetValue(TestConstants.CURRENECY_JPY, out fee);
                Assert.AreEqual(fee, 0L);
                totalBal1.OpenInterestLong.TryGetValue(symbolId, out long openInterest);
                Assert.AreEqual(openInterest, 0L);

                ApiPlaceOrder order102 = ApiPlaceOrder.Builder()
                        .uid(TestConstants.UID_2)
                        .orderId(102)
                        .price(10770L)
                        .reservePrice(10770L)
                        .size(30L)
                        .action(OrderAction.ASK)
                        .orderType(OrderType.IOC)
                        .symbol(symbolId)
                        .build();

                container.submitCommandSync(order102, cmd => Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS));

                // verify buyer maker balance
                container.validateUserState(TestConstants.UID_1, profile=> {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_JPY], 250_000L - makerFee * 30);
                    profile.Accounts.TryGetValue(TestConstants.CURRENECY_USD, out long accountValue);
                    Assert.AreEqual(accountValue, 0L);
                    Assert.AreEqual(profile.Positions[symbolId].direction, PositionDirection.LONG);
                    Assert.AreEqual(profile.Positions[symbolId].openVolume, 30L);
                    Assert.AreEqual(profile.Positions[symbolId].pendingBuySize, 20L);
                    Assert.AreEqual(profile.Positions[symbolId].pendingSellSize, 0L);
                    Assert.False(profile.fetchIndexedOrders().Count == 0);
                });

                // verify seller taker balance
                container.validateUserState(TestConstants.UID_2, profile=> {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_JPY], 200_000L - takerFee * 30);
                    profile.Accounts.TryGetValue(TestConstants.CURRENECY_USD, out long accountValue);
                    Assert.AreEqual(accountValue, 0L);
                    Assert.AreEqual(profile.Positions[symbolId].direction, PositionDirection.SHORT);
                    Assert.AreEqual(profile.Positions[symbolId].openVolume, 30L);
                    Assert.AreEqual(profile.Positions[symbolId].pendingBuySize, 0L);
                    Assert.AreEqual(profile.Positions[symbolId].pendingSellSize, 0L);
                    Assert.True(profile.fetchIndexedOrders().Count == 0);
                });

                // total balance remains the same
                TotalCurrencyBalanceReportResult totalBal2 = container.totalBalanceReport();
                long jpyFees = (makerFee + takerFee) * 30;
                totalBal2.Fees.TryGetValue(TestConstants.CURRENECY_USD, out fee);
                Assert.AreEqual(fee, 0L);
                Assert.AreEqual(totalBal2.Fees[TestConstants.CURRENECY_JPY], jpyFees);
                totalBal2.getClientsBalancesSum().TryGetValue(TestConstants.CURRENECY_USD, out long accountValue);
                Assert.AreEqual(accountValue, 0L);
                Assert.AreEqual(totalBal2.getClientsBalancesSum()[TestConstants.CURRENECY_JPY], jpyAmount1 + jpyAmount2 - jpyFees);
                Assert.AreEqual(totalBal2.OpenInterestLong[symbolId], 30L);
            }
        }

        [Test, Timeout(10_000)]
        public void shouldNotTakeFeesForCancelAsk()
        {

            using (ExchangeTestContainer container = ExchangeTestContainer.create(getPerformanceConfiguration()))
            {
                container.addSymbol(TestConstants.SYMBOLSPECFEE_USD_JPY);

                long jpyAmount1 = 240_000L;
                container.createUserWithMoney(TestConstants.UID_1, TestConstants.CURRENECY_JPY, jpyAmount1);

                ApiPlaceOrder order101 = ApiPlaceOrder.Builder()
                        .uid(TestConstants.UID_1)
                        .orderId(101L)
                        .price(10770L)
                        .reservePrice(0L)
                        .size(40L)
                        .action(OrderAction.ASK)
                        .orderType(OrderType.GTC)
                        .symbol(symbolId)
                        .build();

                container.submitCommandSync(order101, cmd => Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS));

                // verify order placed
                container.validateUserState(TestConstants.UID_1, profile=> {
                    profile.Accounts.TryGetValue(TestConstants.CURRENECY_XBT, out long accountValue);
                    Assert.AreEqual(accountValue, 0L);
                    Assert.AreEqual(profile.fetchIndexedOrders()[101L].Price, order101.Price);
                });


                // verify balance
                container.validateUserState(TestConstants.UID_1, profile=> {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_JPY], 240_000L);
                    profile.Accounts.TryGetValue(TestConstants.CURRENECY_USD, out long accountValue);
                    Assert.AreEqual(accountValue, 0L);
                    Assert.AreEqual(profile.Positions[symbolId].direction, PositionDirection.EMPTY);
                    Assert.AreEqual(profile.Positions[symbolId].openVolume, 0L);
                    Assert.AreEqual(profile.Positions[symbolId].pendingBuySize, 0L);
                    Assert.AreEqual(profile.Positions[symbolId].pendingSellSize, 40L);
                    Assert.False(profile.fetchIndexedOrders().Count == 0);
                });


                // cancel
                container.submitCommandSync(
                        ApiCancelOrder.Builder().orderId(101L).uid(TestConstants.UID_1).symbol(symbolId).build(),
                        CommandResultCode.SUCCESS);

                // verify balance
                container.validateUserState(TestConstants.UID_1, profile=> {
                    Assert.AreEqual(profile.Accounts[TestConstants.CURRENECY_JPY], 240_000L);
                    profile.Accounts.TryGetValue(TestConstants.CURRENECY_USD, out long accountValue);
                    Assert.AreEqual(accountValue, 0L);
                    Assert.True(profile.Positions.Count == 0);
                    Assert.True(profile.fetchIndexedOrders().Count == 0);
                });


                // total balance remains the same
                TotalCurrencyBalanceReportResult totalBal2 = container.totalBalanceReport();
                totalBal2.getClientsBalancesSum().TryGetValue(TestConstants.CURRENECY_USD, out long accountValue);
                Assert.AreEqual(accountValue, 0L);
                Assert.AreEqual(totalBal2.getClientsBalancesSum()[TestConstants.CURRENECY_JPY], 240_000L);
                totalBal2.Fees.TryGetValue(TestConstants.CURRENECY_USD, out long fee);
                Assert.AreEqual(fee, 0L);
                totalBal2.Fees.TryGetValue(TestConstants.CURRENECY_JPY, out fee);
                Assert.AreEqual(fee, 0L);
                totalBal2.OpenInterestLong.TryGetValue(symbolId, out long interest);
                Assert.AreEqual(interest, 0L);
            }
        }


    }
}

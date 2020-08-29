//using Exchange.Core.Common;
//using Exchange.Core.Common.Api;
//using Exchange.Core.Common.Cmd;
//using Exchange.Core.Common.Config;
//using log4net;
//using NUnit.Framework;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using Exchange.Core.Tests.Utils;

//namespace Exchange.Core.Tests.Integration
//{
//    //TODO test cases are moved to cucumber scenarios, remove this class
//    public abstract class ITExchangeCoreIntegration
//    {
//        private static ILog log = LogManager.GetLogger(typeof(ITExchangeCoreIntegration));

//        // configuration provided by child class
//        public abstract PerformanceConfiguration getPerformanceConfiguration();

//        [Test, Timeout(5000)]
//        public void basicFullCycleTestMargin()
//        {
//            basicFullCycleTest(TestConstants.SYMBOLSPEC_EUR_USD);
//        }

//        [Test, Timeout(5000)]
//        public void basicFullCycleTestExchange()
//        {

//            basicFullCycleTest(TestConstants.SYMBOLSPEC_ETH_XBT);
//        }

//        [Test, Timeout(5000)]
//        public void shouldInitSymbols()
//        {
//            using (ExchangeTestContainer container = ExchangeTestContainer.create(getPerformanceConfiguration()))
//            {
//                container.initBasicSymbols();
//            }
//        }

//        [Test, Timeout(5000)]
//        public void shouldInitUsers()
//        {
//            using (ExchangeTestContainer container = ExchangeTestContainer.create(getPerformanceConfiguration()))
//            {
//                container.initBasicUsers();
//            }
//        }


//        // TODO count/verify number of commands and events
//        private void basicFullCycleTest(CoreSymbolSpecification symbolSpec)
//        {

//            using (ExchangeTestContainer container = ExchangeTestContainer.create(getPerformanceConfiguration()))
//            {
//                container.initBasicSymbols();
//                container.initBasicUsers();

//                // ### 1. first user places limit orders
//                ApiPlaceOrder order101 = ApiPlaceOrder.Builder().uid(TestConstants.UID_1).orderId(101).price(1600).size(7).action(OrderAction.ASK).orderType(OrderType.GTC).symbol(symbolSpec.SymbolId).build();

//                log.Debug($"PLACE 101: {order101}");
//                container.submitCommandSync(order101, cmd =>
//                {
//                    Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS);
//                    Assert.AreEqual(cmd.OrderId, 101L);
//                    Assert.AreEqual(cmd.Uid, TestConstants.UID_1);
//                    Assert.AreEqual(cmd.Price, 1600L);
//                    Assert.AreEqual(cmd.Size, 7L);
//                    Assert.AreEqual(cmd.Action, OrderAction.ASK);
//                    Assert.AreEqual(cmd.OrderType, OrderType.GTC);
//                    Assert.AreEqual(cmd.Symbol, symbolSpec.SymbolId);
//                    Assert.IsNull(cmd.MatcherEvent);
//                });

//                int reserve102 = symbolSpec.Type == SymbolType.CURRENCY_EXCHANGE_PAIR ? 1561 : 0;
//                ApiPlaceOrder order102 = ApiPlaceOrder.Builder().uid(TestConstants.UID_1).orderId(102).price(1550).reservePrice(reserve102).size(4)
//                        .action(OrderAction.BID).orderType(OrderType.GTC).symbol(symbolSpec.SymbolId).build();
//                log.Debug($"PLACE 102: {order102}");
//                container.submitCommandSync(order102, cmd=> {
//                    Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS);
//                    Assert.IsNull(cmd.MatcherEvent);
//                });

//                L2MarketDataHelper l2helper = new L2MarketDataHelper().addAsk(1600, 7).addBid(1550, 4);
//                Assert.AreEqual(l2helper.build(), container.requestCurrentOrderBook(symbolSpec.SymbolId));

//                // ### 2. second user sends market order, first order partially matched
//                int reserve201 = symbolSpec.Type == SymbolType.CURRENCY_EXCHANGE_PAIR ? 1800 : 0;
//                ApiPlaceOrder order201 = ApiPlaceOrder.Builder().uid(TestConstants.UID_2).orderId(201).price(1700).reservePrice(reserve201).size(2).action(OrderAction.BID).orderType(OrderType.IOC).symbol(symbolSpec.SymbolId).build();
//                log.Debug($"PLACE 201: {order201}");
//                container.submitCommandSync(order201, cmd=> {
//                    Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS);

//                    List<MatcherTradeEvent> matcherEvents = cmd.extractEvents();
//                    Assert.AreEqual(matcherEvents.Count, 1);

//                    Assert.AreEqual(cmd.Action, OrderAction.BID);
//                    Assert.AreEqual(cmd.OrderId, 201L);
//                    Assert.AreEqual(cmd.Uid, TestConstants.UID_2);

//                    MatcherTradeEvent evt = matcherEvents[0];
//                    Assert.AreEqual(evt.ActiveOrderCompleted, true);
//                    Assert.AreEqual(evt.MatchedOrderId, 101L);
//                    Assert.AreEqual(evt.MatchedOrderUid, TestConstants.UID_1);
//                    Assert.AreEqual(evt.MatchedOrderCompleted, false);
//                    Assert.AreEqual(evt.EventType, MatcherEventType.TRADE);
//                    Assert.AreEqual(evt.Size, 2L);
//                    Assert.AreEqual(evt.Price, 1600L);
//                });

//                // volume is decreased to 5
//                l2helper.setAskVolume(0, 5);
//                Assert.AreEqual(l2helper.build(), container.requestCurrentOrderBook(symbolSpec.SymbolId));


//                // ### 3. second user places limit order
//                int reserve202 = symbolSpec.Type == SymbolType.CURRENCY_EXCHANGE_PAIR ? 1583 : 0;
//                ApiPlaceOrder order202 = ApiPlaceOrder.Builder().uid(TestConstants.UID_2).orderId(202).price(1583).reservePrice(reserve202)
//                        .size(4).action(OrderAction.BID).orderType(OrderType.GTC).symbol(symbolSpec.SymbolId).build();
//                log.Debug($"PLACE 202: {order202}");
//                container.submitCommandSync(order202, cmd=> {
//                    Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS);
//                    Assert.IsNull(cmd.MatcherEvent);
//                    List<MatcherTradeEvent> matcherEvents = cmd.extractEvents();
//                    Assert.AreEqual(matcherEvents.Count, 0);
//                });

//                l2helper.insertBid(0, 1583, 4);
//                Assert.AreEqual(l2helper.build(), container.requestCurrentOrderBook(symbolSpec.SymbolId));


//                // ### 4. first trader moves his order - it will match existing order (202) but not entirely
//                ApiMoveOrder moveOrder = ApiMoveOrder.Builder().symbol(symbolSpec.SymbolId).uid(TestConstants.UID_1).orderId(101).newPrice(1580).build();
//                log.Debug($"MOVE 101: {moveOrder}");
//                container.submitCommandSync(moveOrder, cmd=> {
//                    Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS);

//                    List<MatcherTradeEvent> matcherEvents = cmd.extractEvents();
//                    Assert.AreEqual(matcherEvents.Count, 1);

//                    Assert.AreEqual(cmd.Action, OrderAction.ASK);
//                    Assert.AreEqual(cmd.OrderId, 101L);
//                    Assert.AreEqual(cmd.Uid, TestConstants.UID_1);

//                    MatcherTradeEvent evt = matcherEvents[0];
//                    Assert.AreEqual(evt.ActiveOrderCompleted, false);
//                    Assert.AreEqual(evt.MatchedOrderId, 202L);
//                    Assert.AreEqual(evt.MatchedOrderUid, TestConstants.UID_2);
//                    Assert.AreEqual(evt.MatchedOrderCompleted, true);
//                    Assert.AreEqual(evt.EventType, MatcherEventType.TRADE);
//                    Assert.AreEqual(evt.Size, 4L);
//                    Assert.AreEqual(evt.Price, 1583L);
//                });

//                l2helper.setAskPriceVolume(0, 1580, 1).removeBid(0);
//                Assert.AreEqual(l2helper.build(), container.requestCurrentOrderBook(symbolSpec.SymbolId));

//                Assert.AreEqual(container.totalBalanceReport().isGlobalBalancesAllZero(), true);
//            }
//        }


//        //        [Test, Timeout(5000)]
//        //        public void exchangeRiskBasicTest() throws Exception
//        //        {

//        //                using (final ExchangeTestContainer container = ExchangeTestContainer.create(getPerformanceConfiguration())) {
//        //                    container.initBasicSymbols();
//        //                    container.createUserWithMoney(UID_1, CURRENECY_XBT, 2_000_000); // 2M satoshi (0.02 BTC)

//        //                    // try submit an order - limit BUY 7 lots, price 300K satoshi (30K x10 step) for each lot 100K szabo
//        //                    // should be rejected
//        //                    final ApiPlaceOrder order101 = ApiPlaceOrder.builder().uid(UID_1).orderId(101).price(30_000).reservePrice(30_000)
//        //                            .size(7).action(OrderAction.BID).orderType(GTC).symbol(SYMBOL_EXCHANGE).build();

//        //    container.submitCommandSync(order101, cmd-> {
//        //                        Assert.AreEqual(cmd.resultCode, is (CommandResultCode.RISK_NSF));
//        //});

//        //// verify
//        //container.validateUserState(UID_1, profile-> {
//        //    Assert.AreEqual(profile.getAccounts().get(CURRENECY_XBT), is (2_000_000L));
//        //    assertTrue(profile.fetchIndexedOrders().isEmpty());
//        //});

//        //// add 100K more
//        //container.submitCommandSync(ApiAdjustUserBalance.builder().uid(UID_1).currency(CURRENECY_XBT).amount(100_000).transactionId(223948217349827L).build(), CHECK_SUCCESS);

//        //// submit order again - should be placed
//        //container.submitCommandSync(order101, cmd-> {
//        //    Assert.AreEqual(cmd.resultCode, is (CommandResultCode.SUCCESS));
//        //    Assert.AreEqual(cmd.orderId, is (101L));
//        //    Assert.AreEqual(cmd.uid, is (UID_1));
//        //    Assert.AreEqual(cmd.price, is (30_000L));
//        //    Assert.AreEqual(cmd.reserveBidPrice, is (30_000L));
//        //    Assert.AreEqual(cmd.size, is (7L));
//        //    Assert.AreEqual(cmd.action, is (OrderAction.BID));
//        //    Assert.AreEqual(cmd.orderType, is (GTC));
//        //    Assert.AreEqual(cmd.symbol, is (SYMBOL_EXCHANGE));
//        //    Assert.IsNull(cmd.matcherEvent);
//        //});

//        //// verify order placed with correct reserve price and account balance is updated accordingly
//        //container.validateUserState(UID_1, profile-> {
//        //    Assert.AreEqual(profile.getAccounts().get(CURRENECY_XBT), is (0L));
//        //    Assert.AreEqual(profile.fetchIndexedOrders().get(101L).price, is (30_000L));
//        //});

//        //container.createUserWithMoney(UID_2, CURRENECY_ETH, 699_999); // 699'999 szabo (<~0.7 ETH)
//        //                                                              // try submit an order - sell 7 lots, price 300K satoshi (30K x10 step) for each lot 100K szabo
//        //                                                              // should be rejected
//        //final ApiPlaceOrder order102 = ApiPlaceOrder.builder().uid(UID_2).orderId(102).price(30_000).size(7).action(ASK).orderType(OrderType.IOC).symbol(SYMBOL_EXCHANGE).build();
//        //container.submitCommandSync(order102, cmd-> {
//        //    Assert.AreEqual(cmd.resultCode, is (CommandResultCode.RISK_NSF));
//        //});

//        //// verify order is rejected and account balance is not changed
//        //container.validateUserState(UID_2, profile-> {
//        //    Assert.AreEqual(profile.getAccounts().get(CURRENECY_ETH), is (699_999L));
//        //    assertTrue(profile.fetchIndexedOrders().isEmpty());
//        //});

//        //// add 1 szabo more
//        //container.submitCommandSync(ApiAdjustUserBalance.builder().uid(UID_2).currency(CURRENECY_ETH).amount(1).transactionId(2193842938742L).build(), CHECK_SUCCESS);

//        //// submit order again - should be matched
//        //container.submitCommandSync(order102, cmd-> {
//        //    Assert.AreEqual(cmd.resultCode, is (CommandResultCode.SUCCESS));
//        //    Assert.AreEqual(cmd.orderId, is (102L));
//        //    Assert.AreEqual(cmd.uid, is (UID_2));
//        //    Assert.AreEqual(cmd.price, is (30_000L));
//        //    Assert.AreEqual(cmd.size, is (7L));
//        //    Assert.AreEqual(cmd.action, is (ASK));
//        //    Assert.AreEqual(cmd.orderType, is (OrderType.IOC));
//        //    Assert.AreEqual(cmd.symbol, is (SYMBOL_EXCHANGE));
//        //    assertNotNull(cmd.matcherEvent);
//        //});

//        //container.validateUserState(UID_2, profile-> {
//        //    Assert.AreEqual(profile.getAccounts().get(CURRENECY_XBT), is (2_100_000L));
//        //    Assert.AreEqual(profile.getAccounts().get(CURRENECY_ETH), is (0L));
//        //    assertTrue(profile.fetchIndexedOrders().isEmpty());
//        //});

//        //container.validateUserState(UID_1, profile-> {
//        //    Assert.AreEqual(profile.getAccounts().get(CURRENECY_ETH), is (700_000L));
//        //    Assert.AreEqual(profile.getAccounts().get(CURRENECY_XBT), is (0L));
//        //    assertTrue(profile.fetchIndexedOrders().isEmpty());
//        //});

//        //assertTrue(container.totalBalanceReport().isGlobalBalancesAllZero());
//        //                }
//        //                }

//        //    [Test, Timeout(5000)]
//        //public void exchangeRiskMoveTest() throws Exception
//        //{

//        //                    using (final ExchangeTestContainer container = ExchangeTestContainer.create(getPerformanceConfiguration())) {
//        //        container.initBasicSymbols();
//        //        container.createUserWithMoney(UID_1, CURRENECY_ETH, 100_000_000); // 100M szabo (100 ETH)

//        //        // try submit an order - sell 1001 lots, price 300K satoshi (30K x10 step) for each lot 100K szabo
//        //        // should be rejected
//        //        container.submitCommandSync(ApiPlaceOrder.builder().uid(UID_1).orderId(202).price(30_000).size(1001).action(ASK).orderType(GTC).symbol(SYMBOL_EXCHANGE).build(),
//        //                cmd-> {
//        //            Assert.AreEqual(cmd.resultCode, is (CommandResultCode.RISK_NSF));
//        //        });

//        //        container.validateUserState(UID_1, profile-> {
//        //            Assert.AreEqual(profile.getAccounts().get(CURRENECY_ETH), is (100_000_000L));
//        //            assertTrue(profile.fetchIndexedOrders().isEmpty());
//        //        });

//        //        // submit order again - should be placed
//        //        container.submitCommandSync(
//        //                ApiPlaceOrder.builder().uid(UID_1).orderId(202).price(30_000).size(1000).action(ASK).orderType(GTC).symbol(SYMBOL_EXCHANGE).build(),
//        //                cmd-> {
//        //            Assert.AreEqual(cmd.resultCode, is (CommandResultCode.SUCCESS));
//        //            Assert.AreEqual(cmd.command, is (OrderCommandType.PLACE_ORDER));
//        //            Assert.AreEqual(cmd.orderId, is (202L));
//        //            Assert.AreEqual(cmd.uid, is (UID_1));
//        //            Assert.AreEqual(cmd.price, is (30_000L));
//        //            Assert.AreEqual(cmd.size, is (1000L));
//        //            Assert.AreEqual(cmd.action, is (ASK));
//        //            Assert.AreEqual(cmd.orderType, is (GTC));
//        //            Assert.AreEqual(cmd.symbol, is (SYMBOL_EXCHANGE));
//        //            Assert.IsNull(cmd.matcherEvent);
//        //        });

//        //        container.validateUserState(UID_1, profile-> {
//        //            Assert.AreEqual(profile.getAccounts().get(CURRENECY_ETH), is (0L));
//        //            assertTrue(profile.fetchIndexedOrders().containsKey(202L));
//        //        });

//        //        // move order to higher price - shouldn't be a problem for ASK order
//        //        container.submitCommandSync(
//        //                ApiMoveOrder.builder().uid(UID_1).orderId(202).newPrice(40_000).symbol(SYMBOL_EXCHANGE).build(),
//        //                cmd-> {
//        //            Assert.AreEqual(cmd.resultCode, is (CommandResultCode.SUCCESS));
//        //            Assert.AreEqual(cmd.command, is (OrderCommandType.MOVE_ORDER));
//        //            Assert.AreEqual(cmd.orderId, is (202L));
//        //            Assert.AreEqual(cmd.uid, is (UID_1));
//        //            Assert.AreEqual(cmd.price, is (40_000L));
//        //            Assert.AreEqual(cmd.symbol, is (SYMBOL_EXCHANGE));
//        //            Assert.IsNull(cmd.matcherEvent);
//        //        });

//        //        container.validateUserState(UID_1, profile-> {
//        //            Assert.AreEqual(profile.getAccounts().get(CURRENECY_ETH), is (0L));
//        //            assertTrue(profile.fetchIndexedOrders().containsKey(202L));
//        //        });

//        //        // move order to lower price - shouldn't be a problem as well for ASK order
//        //        container.submitCommandSync(
//        //                ApiMoveOrder.builder().uid(UID_1).orderId(202).newPrice(20_000).symbol(SYMBOL_EXCHANGE).build(),
//        //                cmd-> {
//        //            Assert.AreEqual(cmd.resultCode, is (CommandResultCode.SUCCESS));
//        //            Assert.AreEqual(cmd.command, is (OrderCommandType.MOVE_ORDER));
//        //            Assert.AreEqual(cmd.orderId, is (202L));
//        //            Assert.AreEqual(cmd.uid, is (UID_1));
//        //            Assert.AreEqual(cmd.price, is (20_000L));
//        //            Assert.AreEqual(cmd.symbol, is (SYMBOL_EXCHANGE));
//        //            Assert.IsNull(cmd.matcherEvent);
//        //        });

//        //        container.validateUserState(UID_1, profile-> {
//        //            Assert.AreEqual(profile.getAccounts().get(CURRENECY_ETH), is (0L));
//        //            assertTrue(profile.fetchIndexedOrders().containsKey(202L));
//        //        });

//        //        // create user
//        //        container.createUserWithMoney(UID_2, CURRENECY_XBT, 94_000_000); // 94M satoshi (0.94 BTC)

//        //        // try submit order with reservePrice above funds limit - rejected
//        //        container.submitCommandSync(
//        //                ApiPlaceOrder.builder().uid(UID_2).orderId(203).price(18_000).reservePrice(19_000).size(500).action(OrderAction.BID).orderType(GTC).symbol(SYMBOL_EXCHANGE).build(),
//        //                cmd-> {
//        //            Assert.AreEqual(cmd.resultCode, is (CommandResultCode.RISK_NSF));
//        //        });

//        //        container.validateUserState(UID_2, profile-> {
//        //            Assert.AreEqual(profile.getAccounts().get(CURRENECY_XBT), is (94_000_000L));
//        //            assertTrue(profile.fetchIndexedOrders().isEmpty());
//        //        });

//        //        // submit order with reservePrice below funds limit - should be placed
//        //        container.submitCommandSync(
//        //                ApiPlaceOrder.builder().uid(UID_2).orderId(203).price(18_000).reservePrice(18_500).size(500).action(OrderAction.BID).orderType(GTC).symbol(SYMBOL_EXCHANGE).build(),
//        //                cmd-> {
//        //            Assert.AreEqual(cmd.resultCode, is (CommandResultCode.SUCCESS));
//        //            Assert.AreEqual(cmd.command, is (OrderCommandType.PLACE_ORDER));
//        //            Assert.AreEqual(cmd.orderId, is (203L));
//        //            Assert.AreEqual(cmd.uid, is (UID_2));
//        //            Assert.AreEqual(cmd.price, is (18_000L));
//        //            Assert.AreEqual(cmd.reserveBidPrice, is (18_500L));
//        //            Assert.AreEqual(cmd.size, is (500L));
//        //            Assert.AreEqual(cmd.action, is (OrderAction.BID));
//        //            Assert.AreEqual(cmd.orderType, is (GTC));
//        //            Assert.AreEqual(cmd.symbol, is (SYMBOL_EXCHANGE));
//        //            Assert.IsNull(cmd.matcherEvent);
//        //        });


//        //        // expected balance when 203 placed with reserve price 18_500
//        //        final long ethUid2 = 94_000_000L - 18_500 * 500 * SYMBOLSPEC_ETH_XBT.getQuoteScaleK();

//        //        container.validateUserState(UID_2, profile-> {
//        //            Assert.AreEqual(profile.getAccounts().get(CURRENECY_XBT), is (ethUid2));
//        //            assertTrue(profile.fetchIndexedOrders().containsKey(203L));
//        //        });

//        //        // move order to lower price - shouldn't be a problem for BID order
//        //        container.submitCommandSync(
//        //                ApiMoveOrder.builder().uid(UID_2).orderId(203).newPrice(15_000).symbol(SYMBOL_EXCHANGE).build(),
//        //                cmd-> {
//        //            Assert.AreEqual(cmd.resultCode, is (CommandResultCode.SUCCESS));
//        //            Assert.AreEqual(cmd.command, is (OrderCommandType.MOVE_ORDER));
//        //            Assert.AreEqual(cmd.orderId, is (203L));
//        //            Assert.AreEqual(cmd.uid, is (UID_2));
//        //            Assert.AreEqual(cmd.price, is (15_000L));
//        //            Assert.AreEqual(cmd.symbol, is (SYMBOL_EXCHANGE));
//        //            Assert.IsNull(cmd.matcherEvent);
//        //        });

//        //        container.validateUserState(UID_2, profile-> {
//        //            Assert.AreEqual(profile.getAccounts().get(CURRENECY_XBT), is (ethUid2));
//        //            Assert.AreEqual(profile.fetchIndexedOrders().get(203L).price, is (15_000L));
//        //        });

//        //        // move order to higher price (above limit) - should be rejected
//        //        container.submitCommandSync(
//        //                ApiMoveOrder.builder().uid(UID_2).orderId(203).newPrice(18_501).symbol(SYMBOL_EXCHANGE).build(),
//        //                cmd-> {
//        //            Assert.AreEqual(cmd.resultCode, is (CommandResultCode.MATCHING_MOVE_FAILED_PRICE_OVER_RISK_LIMIT));
//        //        });

//        //        container.validateUserState(UID_2, profile-> {
//        //            Assert.AreEqual(profile.getAccounts().get(CURRENECY_XBT), is (ethUid2));
//        //            Assert.AreEqual(profile.fetchIndexedOrders().get(203L).price, is (15_000L));
//        //        });

//        //        // move order to higher price (equals limit) - should be accepted
//        //        container.submitCommandSync(
//        //                ApiMoveOrder.builder().uid(UID_2).orderId(203).newPrice(18_500).symbol(SYMBOL_EXCHANGE).build(),
//        //                cmd-> {
//        //            Assert.AreEqual(cmd.resultCode, is (CommandResultCode.SUCCESS));
//        //            Assert.AreEqual(cmd.command, is (OrderCommandType.MOVE_ORDER));
//        //            Assert.AreEqual(cmd.orderId, is (203L));
//        //            Assert.AreEqual(cmd.uid, is (UID_2));
//        //            Assert.AreEqual(cmd.price, is (18_500L));
//        //            Assert.AreEqual(cmd.symbol, is (SYMBOL_EXCHANGE));
//        //            Assert.IsNull(cmd.matcherEvent);
//        //        });

//        //        container.validateUserState(UID_2, profile-> {
//        //            Assert.AreEqual(profile.getAccounts().get(CURRENECY_XBT), is (ethUid2));
//        //            Assert.AreEqual(profile.fetchIndexedOrders().get(203L).price, is (18_500L));
//        //        });

//        //        // set second order price to 17'500
//        //        container.submitCommandSync(
//        //                ApiMoveOrder.builder().uid(UID_2).orderId(203).newPrice(17_500).symbol(SYMBOL_EXCHANGE).build(),
//        //                cmd-> {
//        //            Assert.AreEqual(cmd.resultCode, is (CommandResultCode.SUCCESS));
//        //        });

//        //        container.validateUserState(UID_2, profile-> {
//        //            Assert.AreEqual(profile.getAccounts().get(CURRENECY_XBT), is (ethUid2));
//        //            Assert.AreEqual(profile.fetchIndexedOrders().get(203L).price, is (17_500L));
//        //        });

//        //        // move ASK order to lower price 16'900 so it will trigger trades (by maker's price 17_500)
//        //        container.submitCommandSync(
//        //                ApiMoveOrder.builder().uid(UID_1).orderId(202).newPrice(16_900).symbol(SYMBOL_EXCHANGE).build(),
//        //                cmd-> {
//        //            Assert.AreEqual(cmd.resultCode, is (CommandResultCode.SUCCESS));
//        //            Assert.AreEqual(cmd.command, is (OrderCommandType.MOVE_ORDER));
//        //            Assert.AreEqual(cmd.orderId, is (202L));
//        //            Assert.AreEqual(cmd.uid, is (UID_1));
//        //            Assert.AreEqual(cmd.price, is (16_900L));
//        //            Assert.AreEqual(cmd.symbol, is (SYMBOL_EXCHANGE));

//        //            Assert.AreEqual(cmd.action, is (ASK));

//        //            final MatcherTradeEvent evt = cmd.matcherEvent;
//        //            assertNotNull(evt);
//        //            Assert.AreEqual(evt.eventType, is (MatcherEventType.TRADE));
//        //            Assert.AreEqual(evt.activeOrderCompleted, is (false));
//        //            Assert.AreEqual(evt.matchedOrderId, is (203L));
//        //            Assert.AreEqual(evt.matchedOrderUid, is (UID_2));
//        //            Assert.AreEqual(evt.matchedOrderCompleted, is (true));
//        //            Assert.AreEqual(evt.price, is (17_500L)); // user price from maker order
//        //            Assert.AreEqual(evt.bidderHoldPrice, is (18_500L)); // user original reserve price from bidder order (203)
//        //            Assert.AreEqual(evt.size, is (500L));
//        //        });

//        //        // check UID_1 has 87.5M satoshi (17_500 * 10 * 500) and half-filled SELL order
//        //        container.validateUserState(UID_1, profile-> {
//        //            Assert.AreEqual(profile.getAccounts().get(CURRENECY_XBT), is (87_500_000L));
//        //            Assert.AreEqual(profile.getAccounts().get(CURRENECY_ETH), is (0L));
//        //            Assert.AreEqual(profile.fetchIndexedOrders().get(202L).filled, is (500L));
//        //        });

//        //        // check UID_2 has 6.5M satoshi (after 94M), and 50M szabo (10_000 * 500)
//        //        container.validateUserState(UID_2, profile-> {
//        //            Assert.AreEqual(profile.getAccounts().get(CURRENECY_XBT), is (6_500_000L));
//        //            Assert.AreEqual(profile.getAccounts().get(CURRENECY_ETH), is (50_000_000L));
//        //            assertTrue(profile.fetchIndexedOrders().isEmpty());
//        //        });

//        //        // cancel remaining order
//        //        container.submitCommandSync(
//        //                ApiCancelOrder.builder().orderId(202).uid(UID_1).symbol(SYMBOL_EXCHANGE).build(),
//        //                cmd-> {
//        //            Assert.AreEqual(cmd.resultCode, is (CommandResultCode.SUCCESS));
//        //            Assert.AreEqual(cmd.command, is (OrderCommandType.CANCEL_ORDER));
//        //            Assert.AreEqual(cmd.orderId, is (202L));
//        //            Assert.AreEqual(cmd.uid, is (UID_1));
//        //            Assert.AreEqual(cmd.symbol, is (SYMBOL_EXCHANGE));

//        //            Assert.AreEqual(cmd.action, is (ASK));

//        //            final MatcherTradeEvent evt = cmd.matcherEvent;
//        //            assertNotNull(evt);
//        //            Assert.AreEqual(evt.eventType, is (MatcherEventType.REDUCE));
//        //            Assert.AreEqual(evt.size, is (500L));
//        //        });

//        //        // check UID_1 has 87.5M satoshi (17_500 * 10 * 500) and 50M szabo (after 100M)
//        //        container.validateUserState(UID_1, profile-> {
//        //            Assert.AreEqual(profile.getAccounts().get(CURRENECY_XBT), is (87_500_000L));
//        //            Assert.AreEqual(profile.getAccounts().get(CURRENECY_ETH), is (50_000_000L));
//        //            assertTrue(profile.fetchIndexedOrders().isEmpty());
//        //        });

//        //        assertTrue(container.totalBalanceReport().isGlobalBalancesAllZero());
//        //    }
//        //}

//        //[Test, Timeout(5000)]
//        //public void exchangeCancelBid() throws Exception
//        //{

//        //                        using (final ExchangeTestContainer container = ExchangeTestContainer.create(getPerformanceConfiguration())) {
//        //        container.initBasicSymbols();

//        //        // create user
//        //        container.createUserWithMoney(UID_2, CURRENECY_XBT, 94_000_000); // 94M satoshi (0.94 BTC)

//        //        // submit order with reservePrice below funds limit - should be placed
//        //        container.submitCommandSync(
//        //                ApiPlaceOrder.builder().uid(UID_2).orderId(203).price(18_000).reservePrice(18_500).size(500).action(OrderAction.BID).orderType(GTC).symbol(SYMBOL_EXCHANGE).build(),
//        //                cmd-> {
//        //            Assert.AreEqual(cmd.resultCode, is (CommandResultCode.SUCCESS));
//        //        });

//        //        // verify order placed with correct reserve price and account balance is updated accordingly
//        //        container.validateUserState(UID_2, profile-> {
//        //            Assert.AreEqual(profile.getAccounts().get(CURRENECY_XBT), is (94_000_000L - 18_500 * 500 * SYMBOLSPEC_ETH_XBT.getQuoteScaleK()));
//        //            Assert.AreEqual(profile.fetchIndexedOrders().get(203L).reserveBidPrice, is (18_500L));
//        //        });

//        //        // cancel remaining order
//        //        container.submitCommandSync(
//        //                ApiCancelOrder.builder().orderId(203).uid(UID_2).symbol(SYMBOL_EXCHANGE).build(),
//        //                cmd-> {
//        //            Assert.AreEqual(cmd.resultCode, is (CommandResultCode.SUCCESS));
//        //            Assert.AreEqual(cmd.command, is (OrderCommandType.CANCEL_ORDER));
//        //            Assert.AreEqual(cmd.orderId, is (203L));
//        //            Assert.AreEqual(cmd.uid, is (UID_2));
//        //            Assert.AreEqual(cmd.symbol, is (SYMBOL_EXCHANGE));

//        //            Assert.AreEqual(cmd.action, is (OrderAction.BID));

//        //            final MatcherTradeEvent evt = cmd.matcherEvent;
//        //            assertNotNull(evt);
//        //            Assert.AreEqual(evt.eventType, is (MatcherEventType.REDUCE));
//        //            Assert.AreEqual(evt.bidderHoldPrice, is (18_500L));
//        //            Assert.AreEqual(evt.size, is (500L));
//        //        });

//        //        // verify that all 94M satoshi were returned back
//        //        container.validateUserState(UID_2, profile-> {
//        //            Assert.AreEqual(profile.getAccounts().get(CURRENECY_XBT), is (94_000_000L));
//        //            assertTrue(profile.fetchIndexedOrders().isEmpty());
//        //        });

//        //        assertTrue(container.totalBalanceReport().isGlobalBalancesAllZero());
//        //    }
//        //}
//    }
//}

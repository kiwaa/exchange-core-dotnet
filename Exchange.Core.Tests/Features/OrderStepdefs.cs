using Exchange.Core.Common;
using Exchange.Core.Common.Api;
using Exchange.Core.Common.Api.Reports;
using Exchange.Core.Common.Cmd;
using Exchange.Core.Common.Config;
using Exchange.Core.Tests.Utils;
using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace Exchange.Core.Tests.Steps
{
    [Binding]
    public class OrderStepdefs
    {
        private static ILog log = LogManager.GetLogger(typeof(OrderStepdefs));

        //naive
        public static PerformanceConfiguration testPerformanceConfiguration = PerformanceConfiguration.baseBuilder().build();

        private ExchangeTestContainer container = null;

        private List<MatcherTradeEvent> matcherEvents;
        private Dictionary<long, ApiPlaceOrder> orders = new Dictionary<long, ApiPlaceOrder>();

        [Before]
        public void before()
        {
            container = ExchangeTestContainer.create(testPerformanceConfiguration);
            container.initBasicSymbols();
        }

        [After]
        public void after()
        {
            if (container != null)
            {
                container.Dispose();
            }
        }

        [When("A client (.*) places an (.*) order (.*) at (.*)@(.*) \\(type: (.*), symbol: (.*)\\)")]
        public void aClientPlacesAnOrderAtTypeGTCSymbolEUR_USD(long clientId, String side, long orderId, long price, long size,
                                                           String orderType, CoreSymbolSpecification symbol)
        {
            aClientPassAnOrder(clientId, side, orderId, price, size, orderType, symbol, 0, CommandResultCode.SUCCESS);
        }

        [When("A client (.*) places an (.*) order (.*) at (.*)@(.*) \\(type: (.*), symbol: (.*), reservePrice: (.*)\\)")]
        public void aClientPlacesAnOrderAtTypeGTCSymbolEUR_USD(long clientId, String side, long orderId, long price, long size,
                                                           String orderType, CoreSymbolSpecification symbol, long reservePrice)
        {
            aClientPassAnOrder(clientId, side, orderId, price, size, orderType, symbol, reservePrice, CommandResultCode.SUCCESS);
        }

        private void aClientPassAnOrder(long clientId, String side, long orderId, long price, long size, String orderType,
                                        CoreSymbolSpecification symbol, long reservePrice, CommandResultCode resultCode)
        {
            var action = (OrderAction)Enum.Parse(typeof(OrderAction), side);
            var type = (OrderType)Enum.Parse(typeof(OrderType), orderType);
            ApiPlaceOrder.ApiPlaceOrderBuilder builder = ApiPlaceOrder.Builder().uid(clientId).orderId(orderId).price(price).size(size)
                    .action(action).orderType(type)
                    .symbol(symbol.SymbolId);

            if (reservePrice > 0)
            {
                builder.reservePrice(reservePrice);
            }

            ApiPlaceOrder order = builder.build();

            orders[orderId] = order;

            log.Debug($"PLACE : {order}");
            container.api.submitCommandAsyncFullResponse(order).ContinueWith((Task<OrderCommand> task) =>
            {
                var cmd = task.Result;
                Assert.AreEqual(cmd.OrderId, orderId);
                Assert.AreEqual(cmd.ResultCode, resultCode);
                Assert.AreEqual(cmd.Uid, clientId);
                Assert.AreEqual(cmd.Price, price);
                Assert.AreEqual(cmd.Size, size);
                Assert.AreEqual(cmd.Action, action);
                Assert.AreEqual(cmd.OrderType, type);
                Assert.AreEqual(cmd.Symbol, symbol.SymbolId);

                matcherEvents = cmd.extractEvents();
            }).Wait();
        }


        [Then("The order (.*) is partially matched. LastPx: (.*), LastQty: (.*)")]
        public void theOrderIsPartiallyMatchedLastPxLastQty(long orderId, long lastPx, long lastQty)
        {
            theOrderIsMatched(orderId, lastPx, lastQty, false, null);
        }

        [Then("The order (.*) is fully matched. LastPx: (.*), LastQty: (.*), bidderHoldPrice: (.*)")]
        public void theOrderIsFullyMatchedLastPxLastQtyBidderHoldPrice(long orderId, long lastPx, long lastQty, long bidderHoldPrice)
        {
            theOrderIsMatched(orderId, lastPx, lastQty, true, bidderHoldPrice);
        }

        private void theOrderIsMatched(long orderId, long lastPx, long lastQty, bool completed, long? bidderHoldPrice)
        {
            Assert.AreEqual(matcherEvents.Count, 1);

            MatcherTradeEvent evt = matcherEvents[0];
            Assert.AreEqual(evt.MatchedOrderId, orderId);
            Assert.AreEqual(evt.MatchedOrderUid, orders[orderId].Uid);
            Assert.AreEqual(evt.MatchedOrderCompleted, completed);
            Assert.AreEqual(evt.EventType, MatcherEventType.TRADE);
            Assert.AreEqual(evt.Size, lastQty);
            Assert.AreEqual(evt.Price, lastPx);
            if (bidderHoldPrice != null)
            {
                Assert.AreEqual(evt.BidderHoldPrice, bidderHoldPrice.Value);
            }
        }

        [Then("No trade events")]
        public void noTradeEvents()
        {
            Assert.AreEqual(0, matcherEvents.Count);
        }

        [When("A client (.*) moves a price to (.*) of the order (.*)")]
        public void aClientMovesAPriceToOfTheOrder(long clientId, long newPrice, long orderId)
        {
            moveOrder(clientId, newPrice, orderId, CommandResultCode.SUCCESS);
        }

        [When("A client (.*) could not move a price to (.*) of the order (.*) due to (.*)")]
        public void aClientCouldNotMoveOrder(long clientId, long newPrice, long orderId, String resultCode)
        {
            var tmp = (CommandResultCode)Enum.Parse(typeof(CommandResultCode), resultCode);
            moveOrder(clientId, newPrice, orderId, tmp);
        }

        private void moveOrder(long clientId, long newPrice, long orderId, CommandResultCode resultCode2)
        {
            ApiPlaceOrder initialOrder = orders[orderId];

            ApiMoveOrder moveOrder = ApiMoveOrder.Builder().symbol(initialOrder.Symbol).uid(clientId).orderId(orderId)
                    .newPrice(newPrice).build();
            log.Debug($"MOVE : {moveOrder}");
            container.submitCommandSync(moveOrder, cmd =>
            {
                Assert.AreEqual(cmd.ResultCode, resultCode2);
                Assert.AreEqual(cmd.OrderId, orderId);
                Assert.AreEqual(cmd.Uid, clientId);

                matcherEvents = cmd.extractEvents();
            });
        }

        [Then("The order (.*) is fully matched. LastPx: (.*), LastQty: (.*)")]
        public void theOrderIsFullyMatchedLastPxLastQty(long orderId, long lastPx, long lastQty)
        {
            theOrderIsMatched(orderId, lastPx, lastQty, true, null);
        }

        [Then("An (.*) order book is:")]
        public void an_order_book_is(CoreSymbolSpecification symbol, L2MarketDataHelper orderBook)
        {
            Assert.AreEqual(orderBook.build(), container.requestCurrentOrderBook(symbol.SymbolId));
        }

        [Given("New client (.*) has a balance:")]
        public void newClientAHasABalance(long clientId, List<List<String>> balance)
        {
            List<ApiCommand> cmds = new List<ApiCommand>();

            cmds.Add(ApiAddUser.Builder().uid(clientId).build());

            int transactionId = 0;

            foreach (List<String> entry in balance)
            {
                transactionId++;
                cmds.Add(ApiAdjustUserBalance.Builder().uid(clientId).transactionId(transactionId)
                        .amount(long.Parse(entry[1]))
                        .currency(TestConstants.getCurrency(entry[0]))
                        .build());
            }

            container.api.submitCommandsSync(cmds);

        }

        [When("A client (.*) could not place an (.*) order (.*) at (.*)@(.*) \\(type: (.*), symbol: (.*), reservePrice: (.*)\\) due to (.*)")]
        public void aClientCouldNotPlaceOrder(long clientId, String side, long orderId, long price, long size,
                                          String orderType, CoreSymbolSpecification symbol, long reservePrice, String resultCode)
        {
            var tmp = (CommandResultCode)Enum.Parse(typeof(CommandResultCode), resultCode);
            aClientPassAnOrder(clientId, side, orderId, price, size, orderType, symbol, reservePrice, tmp);
        }

        [Then("A balance of a client (.*):")]
        public void aCurrentBalanceOfAClientA(long clientId, List<List<string>> balance)
        {
            SingleUserReportResult profile = container.getUserProfile(clientId);
            foreach (List<String> record in balance)
            {
                profile.Accounts.TryGetValue(TestConstants.getCurrency(record[0]), out long tmp);
                Assert.AreEqual(tmp, long.Parse(record[1]), "Unexpected balance of: " + record[0]);
            }
        }

        [Then("A client (.*) orders:")]
        public void aCurrentOrdersOfAClientA(long clientId, List<List<String>> table)
        {

            //| id | price | size | filled | reservePrice | side |

            SingleUserReportResult profile = container.getUserProfile(clientId);

            //skip a header if it presents
            Dictionary<String, int> fieldNameByIndex = new Dictionary<String, int>();

            //read a header
            int i = 0;
            foreach (String field in table[0])
            {
                fieldNameByIndex[field] = i++;
            }

            //remove header
            table = table.Skip(1).ToList();

            Dictionary<long, Order> orders = profile.fetchIndexedOrders();

            foreach (List<String> record in table)
            {
                long orderId = long.Parse(record[fieldNameByIndex["id"]]);
                Order order = orders[orderId];
                Assert.NotNull(order);

                checkField(fieldNameByIndex, record, "price", order.Price);
                checkField(fieldNameByIndex, record, "size", order.Size);
                checkField(fieldNameByIndex, record, "filled", order.Filled);
                checkField(fieldNameByIndex, record, "reservePrice", order.ReserveBidPrice);

                if (fieldNameByIndex.ContainsKey("side"))
                {
                    var action = (OrderAction)Enum.Parse(typeof(OrderAction), record[fieldNameByIndex["side"]]);
                    Assert.AreEqual(action, order.Action, "Unexpected action");
                }

            }
        }

        private void checkField(Dictionary<String, int> fieldNameByIndex, List<String> record, String field, long expected)
        {
            if (fieldNameByIndex.ContainsKey(field))
            {
                long actual = long.Parse(record[fieldNameByIndex[field]]);
                Assert.AreEqual(actual, expected, "Unexpected value for " + field);
            }
        }

        [Then("A client (.*) does not have active orders")]
        public void aClientBDoesNotHaveActiveOrders(long clientId)
        {
            SingleUserReportResult profile = container.getUserProfile(clientId);
            Assert.AreEqual(0, profile.fetchIndexedOrders().Count);
        }

        [Given("(.*) (.*) is added to the balance of a client (.*)")]
        public void xbtIsAddedToTheBalanceOfAClientA(long ammount, String currency, long clientId)
        {

            // add 1 szabo more
            container.submitCommandSync(ApiAdjustUserBalance.Builder()
                    .uid(clientId)
                    .currency(TestConstants.getCurrency(currency))
                    .amount(ammount).transactionId(2193842938742L).build(), ExchangeTestContainer.CHECK_SUCCESS);
        }


        [When("A client (.*) cancels the remaining size (.*) of the order (.*)")]
        public void aClientACancelsTheOrder(long clientId, long size, long orderId)
        {

            ApiPlaceOrder initialOrder = orders[orderId];

            ApiCancelOrder order = ApiCancelOrder.Builder().orderId(orderId).uid(clientId).symbol(initialOrder.Symbol).build();

            container.api.submitCommandAsyncFullResponse(order).ContinueWith((Task<OrderCommand> task) =>
            {
                var cmd = task.Result;
                Assert.AreEqual(cmd.ResultCode, CommandResultCode.SUCCESS);
                Assert.AreEqual(cmd.Command, OrderCommandType.CANCEL_ORDER);
                Assert.AreEqual(cmd.OrderId, orderId);
                Assert.AreEqual(cmd.Uid, clientId);
                Assert.AreEqual(cmd.Symbol, initialOrder.Symbol);
                Assert.AreEqual(cmd.Action, initialOrder.Action);

                MatcherTradeEvent evt = cmd.MatcherEvent;
                Assert.NotNull(evt);
                Assert.AreEqual(evt.EventType, MatcherEventType.REDUCE);
                Assert.AreEqual(evt.Size, size);
            }).Wait();
        }
    }
}

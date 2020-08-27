using Exchange.Core.Common;
using Exchange.Core.Orderbook;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Exchange.Core.Collections.Utils;

namespace Exchange.Core.Tests.Core.OrderBook
{
    [TestFixture]
    public sealed class OrdersBucketNaiveTest
    {

        private static readonly int PRICE = 1000;
        private static readonly int UID_1 = 412;
        private static readonly int UID_2 = 413;
        private static readonly int UID_9 = 419;

        private readonly OrderBookEventsHelper eventsHelper = new OrderBookEventsHelper(() => new MatcherTradeEvent());

        private OrdersBucketNaive bucket;

        [SetUp]
        public void beforeGlobal()
        {

            bucket = new OrdersBucketNaive(PRICE);

            bucket.put(Order.Builder().orderId(1).uid(UID_1).size(100).build());
            Assert.AreEqual(bucket.getNumOrders(), 1);
            Assert.AreEqual(bucket.TotalVolume, 100L);

            bucket.validate();

            bucket.put(Order.Builder().orderId(2).uid(UID_2).size(40).build());
            Assert.AreEqual(bucket.getNumOrders(), 2);
            Assert.AreEqual(bucket.TotalVolume, 140L);

            bucket.validate();

            bucket.put(Order.Builder().orderId(3).uid(UID_1).size(1).build());
            Assert.AreEqual(bucket.getNumOrders(), 3);
            Assert.AreEqual(bucket.TotalVolume, 141L);

            bucket.validate();

            bucket.remove(2, UID_2);
            Assert.AreEqual(bucket.getNumOrders(), 2);
            Assert.AreEqual(bucket.TotalVolume, 101L);

            bucket.validate();

            bucket.put(Order.Builder().orderId(4).uid(UID_1).size(200).build());
            Assert.AreEqual(bucket.getNumOrders(), 3);
            Assert.AreEqual(bucket.TotalVolume, 301L);
        }

        [Test]
        public void shouldAddOrder()
        {
            bucket.put(Order.Builder().orderId(5).uid(UID_2).size(240).build());

            Assert.AreEqual(bucket.getNumOrders(), 4);
            Assert.AreEqual(bucket.TotalVolume, 541L);
        }


        [Test]
        public void shouldRemoveOrders()
        {

            Order removed = bucket.remove(1, UID_1);
            Assert.NotNull(removed);
            Assert.AreEqual(bucket.getNumOrders(), 2);
            Assert.AreEqual(bucket.TotalVolume, 201L);

            removed = bucket.remove(4, UID_1);
            Assert.NotNull(removed);
            Assert.AreEqual(bucket.getNumOrders(), 1);
            Assert.AreEqual(bucket.TotalVolume, 1L);

            // can not remove existing order
            removed = bucket.remove(4, UID_1);
            Assert.IsNull(removed);
            Assert.AreEqual(bucket.getNumOrders(), 1);
            Assert.AreEqual(bucket.TotalVolume, 1L);

            removed = bucket.remove(3, UID_1);
            Assert.NotNull(removed);
            Assert.AreEqual(bucket.getNumOrders(), 0);
            Assert.AreEqual(bucket.TotalVolume, 0L);
        }


        [Test]
        public void shouldAddManyOrders()
        {
            int numOrdersToAdd = 100_000;
            long expectedVolume = bucket.TotalVolume;
            int expectedNumOrders = bucket.getNumOrders() + numOrdersToAdd;
            for (int i = 0; i < numOrdersToAdd; i++)
            {
                bucket.put(Order.Builder().orderId(i + 5).uid(UID_2).size(i).build());
                expectedVolume += i;
            }

            Assert.AreEqual(bucket.getNumOrders(), expectedNumOrders);
            Assert.AreEqual(bucket.TotalVolume, expectedVolume);
        }

        [Test]
        public void shouldAddAndRemoveManyOrders()
        {
            int numOrdersToAdd = 100;
            long expectedVolume = bucket.TotalVolume;
            int expectedNumOrders = bucket.getNumOrders() + numOrdersToAdd;

            List<Order> orders = new List<Order>(numOrdersToAdd);
            for (int i = 0; i < numOrdersToAdd; i++)
            {
                Order order = Order.Builder().orderId(i + 5).uid(UID_2).size(i).build();
                orders.Add(order);
                bucket.put(order);
                expectedVolume += i;
            }

            Assert.AreEqual(bucket.getNumOrders(), expectedNumOrders);
            Assert.AreEqual(bucket.TotalVolume, expectedVolume);

            orders.Shuffle(new Random(1));

            foreach (var order in orders)
            {
                bucket.remove(order.OrderId, UID_2);
                expectedNumOrders--;
                expectedVolume -= order.Size;
                Assert.AreEqual(bucket.getNumOrders(), expectedNumOrders);
                Assert.AreEqual(bucket.TotalVolume, expectedVolume);
            }

        }


        [Test]
        public void shouldMatchAllOrders()
        {
            int numOrdersToAdd = 100;
            long expectedVolume = bucket.TotalVolume;
            int expectedNumOrders = bucket.getNumOrders() + numOrdersToAdd;

            int orderId = 5;

            List<Order> orders = new List<Order>(numOrdersToAdd);
            for (int i = 0; i < numOrdersToAdd; i++)
            {
                Order order = Order.Builder().orderId(orderId++).uid(UID_2).size(i).build();
                orders.Add(order);
                bucket.put(order);
                expectedVolume += i;
            }

            Assert.AreEqual(bucket.getNumOrders(), expectedNumOrders);
            Assert.AreEqual(bucket.TotalVolume, expectedVolume);

            orders.Shuffle(new Random(1));
            
            List<Order> orders1 = orders.Take(80).ToList();

            foreach (var order in orders1)
            {
                bucket.remove(order.OrderId, UID_2);
                expectedNumOrders--;
                expectedVolume -= order.Size;
                Assert.AreEqual(bucket.getNumOrders(), expectedNumOrders);
                Assert.AreEqual(bucket.TotalVolume, expectedVolume);
            }

            OrderCommand triggerOrd = OrderCommand.update(8182, UID_9, 1000);
            OrdersBucketNaive.MatcherResult matcherResult = bucket.match(expectedVolume, triggerOrd, eventsHelper);

            Assert.AreEqual(MatcherTradeEvent.asList(matcherResult.EventsChainHead).Count, expectedNumOrders);

            Assert.AreEqual(bucket.getNumOrders(), 0);
            Assert.AreEqual(bucket.TotalVolume, 0L);

            bucket.getNumOrders();

        }

        //[Test]
        //public void shouldMatchAllOrders2()
        //{
        //    int numOrdersToAdd = 1000;
        //    long expectedVolume = bucket.TotalVolume;
        //    int expectedNumOrders = bucket.getNumOrders();

        //    bucket.validate();
        //    int orderId = 5;

        //    for (int j = 0; j < 100; j++)
        //    {
        //        List<Order> orders = new ArrayList<>(numOrdersToAdd);
        //        for (int i = 0; i < numOrdersToAdd; i++)
        //        {
        //            Order order = Order.builder().orderId(orderId++).uid(UID_2).size(i).build();
        //            orders.add(order);

        //            bucket.put(order);
        //            expectedNumOrders++;
        //            expectedVolume += i;

        //            //log.debug("{}-{}: orderId:{}", j, i, orderId);

        //            bucket.validate();
        //        }

        //        Assert.AreEqual(bucket.getNumOrders(), is (expectedNumOrders));
        //        Assert.AreEqual(bucket.TotalVolume, is (expectedVolume));

        //        Collections.shuffle(orders, new Random(1));

        //        List<Order> orders1 = orders.subList(0, 900);

        //        for (Order order : orders1)
        //        {
        //            bucket.remove(order.orderId, UID_2);
        //            expectedNumOrders--;
        //            expectedVolume -= order.size;
        //            Assert.AreEqual(bucket.getNumOrders(), is (expectedNumOrders));
        //            Assert.AreEqual(bucket.TotalVolume, is (expectedVolume));

        //            bucket.validate();
        //        }

        //        long toMatch = expectedVolume / 2;

        //        OrderCommand triggerOrd = OrderCommand.update(119283900, UID_9, 1000);

        //        OrdersBucketNaive.MatcherResult matcherResult = bucket.match(toMatch, triggerOrd, eventsHelper);
        //        long totalVolume = matcherResult.volume;
        //        Assert.AreEqual(totalVolume, is (toMatch));
        //        expectedVolume -= totalVolume;
        //        Assert.AreEqual(bucket.TotalVolume, is (expectedVolume));
        //        expectedNumOrders = bucket.getNumOrders();

        //        bucket.validate();
        //    }

        //    OrderCommand triggerOrd = OrderCommand.update(1238729387, UID_9, 1000);

        //    OrdersBucketNaive.MatcherResult matcherResult = bucket.match(expectedVolume, triggerOrd, eventsHelper);

        //    Assert.AreEqual(MatcherTradeEvent.asList(matcherResult.eventsChainHead).size(), is (expectedNumOrders));

        //    Assert.AreEqual(bucket.getNumOrders(), is (0));
        //    Assert.AreEqual(bucket.TotalVolume, is (0L));

        //    bucket.getNumOrders();

        //}


    }

}

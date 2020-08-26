using log4net;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Exchange.Core.Collections.Tests
{
    [TestFixture]
    public class LongAdaptiveRadixTreeMapTest
    {
        private static ILog log = LogManager.GetLogger(typeof(LongAdaptiveRadixTreeMapTest));

        private LongAdaptiveRadixTreeMap<String> map;
        private SortedDictionary<long, String> origMap;

        [SetUp]
        public void SetUp()
        {
            map = new LongAdaptiveRadixTreeMap<string>();
            origMap = new SortedDictionary<long, string>();
        }

        [Test]
        public void shouldPerformBasicOperations()
        {
            map.validateInternalState();
            Assert.IsNull(map.get(0));
            map.put(2, "two");
            map.validateInternalState();
            //Assert.AreEqual(map.get(2), is("two"));
            map.put(223, "dds");
            map.put(49, "fn");
            map.put(1, "fn");

            //        System.out.println(String.format("11239847219L = %016X", 11239847219L));
            //        System.out.println(String.format("1123909L = %016X", 1123909L));
            map.put(long.MaxValue, "fn");
            map.put(11239847219L, "11239847219L");
            map.put(1123909L, "1123909L");
            map.put(11239837212L, "11239837212L");
            map.put(13213, "13213");
            map.put(13423, "13423");

            //        System.out.println(map.printDiagram());

            Assert.AreEqual(map.get(223), "dds");
            Assert.AreEqual(map.get(long.MaxValue), "fn");
            Assert.AreEqual(map.get(11239837212L), "11239837212L");


            //        System.out.println(map.printDiagram());

        }

        [Test]
        public void shouldCallForEach()
        {
            map.put(533, "533");
            map.put(573, "573");
            map.put(38234, "38234");
            map.put(38251, "38251");
            map.put(38255, "38255");
            map.put(40001, "40001");
            map.put(40021, "40021");
            map.put(40023, "40023");
            //        System.out.println(map.printDiagram());
            List<long> keys = new List<long>();
            List<string> values = new List<string>();
            Action<long, string> consumer = (k, v) =>
            {
                keys.Add(k);
                values.Add(v);
            };
            long[] keysArray = { 533L, 573L, 38234L, 38251L, 38255L, 40001L, 40021L, 40023L };
            String[] valuesArray = { "533", "573", "38234", "38251", "38255", "40001", "40021", "40023" };
            List<long> keysExpected = keysArray.ToList();
            List<String> valuesExpected = valuesArray.ToList();
            List<long> keysExpectedRev = keysArray.ToList();
            List<String> valuesExpectedRev = valuesArray.ToList();
            keysExpectedRev.Reverse();
            valuesExpectedRev.Reverse();
            //Collections.reverse();
            //Collections.reverse();

            map.forEach(consumer, int.MaxValue);
            CollectionAssert.AreEqual(keys, keysExpected);
            CollectionAssert.AreEqual(values, valuesExpected);
            keys.Clear();
            values.Clear();

            map.forEach(consumer, 8);
            CollectionAssert.AreEqual(keys, keysExpected);
            CollectionAssert.AreEqual(values, valuesExpected);
            keys.Clear();
            values.Clear();

            map.forEach(consumer, 3);
            CollectionAssert.AreEqual(keys, keysExpected.Take(3).ToList());
            CollectionAssert.AreEqual(values, valuesExpected.Take(3).ToList());
            keys.Clear();
            values.Clear();

            map.forEach(consumer, 0);
            CollectionAssert.AreEqual(keys, new long[0]);
            CollectionAssert.AreEqual(values, new long[0]);
            keys.Clear();
            values.Clear();


            map.forEachDesc(consumer, int.MaxValue);
            CollectionAssert.AreEqual(keys, keysExpectedRev);
            CollectionAssert.AreEqual(values, valuesExpectedRev);
            keys.Clear();
            values.Clear();

            map.forEachDesc(consumer, 8);
            CollectionAssert.AreEqual(keys, keysExpectedRev);
            CollectionAssert.AreEqual(values, valuesExpectedRev);
            keys.Clear();
            values.Clear();

            map.forEachDesc(consumer, 3);
            CollectionAssert.AreEqual(keys, keysExpectedRev.Take(3));
            CollectionAssert.AreEqual(values, valuesExpectedRev.Take(3));
            keys.Clear();
            values.Clear();

            map.forEachDesc(consumer, 0);
            CollectionAssert.AreEqual(keys, new long[0]);
            CollectionAssert.AreEqual(values, new long[0]);
            keys.Clear();
            values.Clear();
        }

        [Test]
        public void shouldFindHigherKeys()
        {

            map.put(33, "33");
            map.put(273, "273");
            map.put(182736400230L, "182736400230");
            map.put(182736487234L, "182736487234");
            map.put(37, "37");
            //        System.out.println(map.printDiagram());

            //        Assert.AreEqual(map.getHigherValue(63120L), is(String.valueOf(182736400230L)));
            //        Assert.AreEqual(map.getHigherValue(255), is(String.valueOf("273")));
            //
            for (int x = 37; x < 273; x++)
            {
                //            log.Debug("TRY:{} {}", x, String.format("%Xh", x));
                Assert.AreEqual(map.getHigherValue(x), "273");
            }
            //
            for (int x = 273; x < 100000; x++)
            {
                //            log.Debug("TRY:{} {}", x, String.format("%Xh", x));
                Assert.AreEqual(map.getHigherValue(x), "182736400230");
            }
            //            log.Debug("TRY:{} {}", 182736388198L, String.format("%Xh", 182736388198L));
            Assert.AreEqual(map.getHigherValue(182736388198L), "182736400230");

            for (long x = 182736300230L; x < 182736400229L; x++)
            {
                //            log.Debug("TRY:{} {}", x, String.format("%Xh", x));
                Assert.AreEqual(map.getHigherValue(x), "182736400230");
            }
            for (long x = 182736400230L; x < 182736487234L; x++)
            {
                //            log.Debug("TRY:{} {}", x, String.format("%Xh", x));
                Assert.AreEqual(map.getHigherValue(x), "182736487234");
            }
            for (long x = 182736487234L; x < 182736497234L; x++)
            {
                //            log.Debug("TRY:{} {}", x, String.format("%Xh", x));
                Assert.IsNull(map.getHigherValue(x));
            }

        }

        [Test]
        public void shouldFindLowerKeys()
        {

            map.put(33, "33");
            map.put(273, "273");
            map.put(182736400230L, "182736400230");
            map.put(182736487234L, "182736487234");
            map.put(37, "37");
            //        System.out.println(map.printDiagram());

            Assert.AreEqual(map.getLowerValue(63120L), 273L.ToString());
            Assert.AreEqual(map.getLowerValue(255), "37");
            Assert.AreEqual(map.getLowerValue(275), "273");

            Assert.IsNull(map.getLowerValue(33));
            Assert.IsNull(map.getLowerValue(32));
            for (int x = 34; x <= 37; x++)
            {
                //            log.Debug("TRY:{} {}", x, String.format("%Xh", x));
                Assert.AreEqual(map.getLowerValue(x), "33");
            }
            for (int x = 38; x <= 273; x++)
            {
                //            log.Debug("TRY:{} {}", x, String.format("%Xh", x));
                Assert.AreEqual(map.getLowerValue(x), "37");
            }
            //
            for (int x = 274; x < 100000; x++)
            {
                //            log.Debug("TRY:{} {}", x, String.format("%Xh", x));
                Assert.AreEqual(map.getLowerValue(x), "273");
            }

            //            log.Debug("TRY:{} {}", 182736388198L, String.format("%Xh", 182736388198L));
            Assert.AreEqual(map.getLowerValue(182736487334L), "182736487234");

            for (long x = 182736300230L; x < 182736400230L; x++)
            {
                //            log.Debug("TRY:{} {}", x, String.format("%Xh", x));
                Assert.AreEqual(map.getLowerValue(x), "273");
            }
            //        for (long x = 182736400230L; x < 182736487234L; x++) {
            ////            log.Debug("TRY:{} {}", x, String.format("%Xh", x));
            //            Assert.AreEqual(map.getHigherValue(x), is("182736487234"));
            //        }
            //        for (long x = 182736487234L; x < 182736497234L; x++) {
            ////            log.Debug("TRY:{} {}", x, String.format("%Xh", x));
            //            Assert.IsNull(map.getHigherValue(x));
            //        }

        }


        [Test]
        public void shouldCompactNodes()
        {
            put(2, "2");
            //        System.out.println(map.printDiagram());
            Assert.AreEqual(map.get(2), "2");
            Assert.IsNull(map.get(3));
            Assert.IsNull(map.get(256 + 2));
            Assert.IsNull(map.get(256 * 256 * 256 + 2));
            Assert.IsNull(map.get(long.MaxValue - 0xFF + 2));

            //        map.put(0x010002L, "0x010002");
            //        map.put(0xFF0002L, "0xFF0002");
            //        map.put(long.MaxValue, "MAX_VALUE");
            put(0x414F32L, "0x414F32");
            put(0x414F33L, "0x414F33");
            put(0x414E00L, "0x414E00");
            put(0x407654L, "0x407654");
            put(0x33558822DD44AA11L, "0x33558822DD44AA11");
            put(0xFFFFFFFFFFFFFFL, "0xFFFFFFFFFFFFFF");
            put(0xFFFFFFFFFFFFFEL, "0xFFFFFFFFFFFFFE");
            put(0x112233445566L, "0x112233445566");
            put(0x1122AAEE5566L, "0x1122AAEE5566");
            //        System.out.println(map.printDiagram());

            Assert.AreEqual(map.get(0x414F32L), "0x414F32");
            Assert.AreEqual(map.get(0x414F33L), "0x414F33");
            Assert.AreEqual(map.get(0x414E00L), "0x414E00");
            Assert.IsNull(map.get(0x414D00L));
            Assert.IsNull(map.get(0x414D33L));
            Assert.IsNull(map.get(0x414D32L));
            Assert.IsNull(map.get(0x424F32L));

            Assert.AreEqual(map.get(0x407654L), "0x407654");
            Assert.AreEqual(map.get(0x33558822DD44AA11L), "0x33558822DD44AA11");
            Assert.IsNull(map.get(0x00558822DD44AA11L));
            Assert.AreEqual(map.get(0xFFFFFFFFFFFFFFL), "0xFFFFFFFFFFFFFF");
            Assert.AreEqual(map.get(0xFFFFFFFFFFFFFEL), "0xFFFFFFFFFFFFFE");
            Assert.IsNull(map.get(0xFFFFL));
            Assert.IsNull(map.get(0xFFL));
            Assert.AreEqual(map.get(0x112233445566L), "0x112233445566");
            Assert.AreEqual(map.get(0x1122AAEE5566L), "0x1122AAEE5566");
            Assert.IsNull(map.get(0x112333445566L));
            Assert.IsNull(map.get(0x112255445566L));
            Assert.IsNull(map.get(0x112233EE5566L));
            Assert.IsNull(map.get(0x1122AA445566L));

            //        map.remove(0x112233445566L);
            remove(0x414F32L);
            remove(0x414E00L);
            remove(0x407654L);
            remove(2);
            //        System.out.println(map.printDiagram());

        }

        [Test]
        public void shouldExtendTo16andReduceTo4()
        {
            put(2, "2");
            put(223, "223");
            put(49, "49");
            put(1, "1");
            // 4->16
            put(77, "77");
            put(4, "4");

            remove(223);
            remove(1);
            // 16->4
            remove(4);
            remove(49);

            // reduce intermediate

            put(65536 * 7, "65536*7");
            put(65536 * 3, "65536*3");
            put(65536 * 2, "65536*2");
            // 4->16
            put(65536 * 4, "65536*4");
            put(65536 * 3 + 3, "65536*3+3");

            remove(65536 * 2);
            // 16->4
            remove(65536 * 4);
            remove(65536 * 7);
            //        System.out.println(map.printDiagram());
        }

        [Test]
        public void shouldExtendTo48andReduceTo16()
        {
            // reduce at end level

            for (int i = 0; i < 16; i++)
            {
                put(i, "" + i);
            }
            // 16->48
            put(177, "177");
            put(56, "56");
            put(255, "255");

            remove(0);
            remove(16);
            remove(13);
            remove(17); // nothing
            remove(3);
            remove(5);
            remove(255);
            remove(7);
            // 48->16
            remove(8);
            remove(2);
            remove(38);
            put(4, "4A");


            // reduce intermediate

            for (int i = 0; i < 16; i++)
            {
                put(256 * i, "" + 256 * i);
            }


            // 16->48
            put(256 * 47, "" + 256 * 47);
            put(256 * 27, "" + 256 * 27);
            put(256 * 255, "" + 256 * 255);
            put(256 * 22, "" + 256 * 22);


            remove(256 * 5);
            remove(256 * 6);
            remove(256 * 7);
            remove(256 * 8);
            remove(256 * 9);
            remove(256 * 10);
            remove(256 * 11);
            // 48->16
            remove(256 * 15);
            remove(256 * 13);
            remove(256 * 14);
            remove(256 * 12);

            //        System.out.println(map.printDiagram());
        }


        [Test]
        public void shouldExtendTo256andReduceTo48()
        {
            // reduce at end level
            for (int i = 0; i < 48; i++)
            {
                int key = 255 - i * 3;
                put(key, "" + key);
            }

            //        // 48->256
            put(176, "176");
            put(221, "221");

            remove(252);
            remove(132);
            remove(135);
            remove(138);
            remove(141);
            remove(144);
            remove(147);
            remove(150);
            remove(153);
            remove(156);
            remove(159);
            remove(162);
            remove(165);

            for (int i = 0; i < 50; i++)
            {
                int key = 65536 * (13 + i * 3);
                put(key, "" + key);
            }

            for (int i = 10; i < 30; i++)
            {
                int key = 65536 * (13 + i * 3);
                remove(key);
            }

            //        System.out.println(map.printDiagram());
        }

        //// C# : value type is not supported currently
        //[Test]
        //public void shouldLoadManyItems()
        //{

        //    Random rand = new Random(1);

        //    Func<int, int> stepFunction = i => 1 + rand.Next((int)Math.Min(int.MaxValue, 1L + (long.highestOneBit(i) >> 8)));
        //    //        final UnaryOperator<Integer> stepFunction = i->1;
        //    //        final UnaryOperator<Integer> stepFunction = i->1+rand.nextInt(Integer.MAX_VALUE);

        //    int forEachSize = 5000;

        //    List<long> forEachKeysArt = new List<long>(forEachSize);
        //    List<long> forEachValuesArt = new List<long>(forEachSize);
        //    Action<long, long> forEachConsumerArt = (k, v) =>
        //    {
        //        forEachKeysArt.Add(k);
        //        forEachValuesArt.Add(v);
        //    };

        //    List<long> forEachKeysBst = new List<long>(forEachSize);
        //    List<long> forEachValuesBst = new List<long>(forEachSize);
        //    Action<long, long> forEachConsumerBst = (k, v) =>
        //    {
        //        forEachKeysBst.Add(k);
        //        forEachValuesBst.Add(v);
        //    };

        //    LongAdaptiveRadixTreeMap<long> art = new LongAdaptiveRadixTreeMap<long>();

        //    SortedDictionary<long, long> bst = new SortedDictionary<long, long>();

        //    //            int num = 500_000;
        //    int num = 100_000;
        //    List<long> list = new List<long>(num);
        //    long j = 0;
        //    log.Debug("generate random numbers..");
        //    long offset = 1_000_000_000L + rand.Next(1_000_000);
        //    for (int i = 0; i < num; i++)
        //    {
        //        list.Add(offset + j);
        //        j += stepFunction(i);
        //    }
        //    log.Debug("shuffle..");
        //    Collections.shuffle(list, rand);

        //    log.Debug("put into BST..");
        //    list.ForEach(x => bst[x] = x);

        //    log.Debug("put into ADT..");
        //    //            list.forEach(x -> log.Debug("{}", x));

        //    list.ForEach(x => art.put(x, x));

        //    log.Debug("shuffle..");
        //    Collections.shuffle(list, rand);

        //    log.Debug("get (hit) from BST..");
        //    long sum = 0;
        //    foreach (long x in list)
        //    {
        //        sum += bst[x];
        //    }

        //    log.Debug("get (hit) from ADT..");
        //    foreach (long x in list)
        //    {
        //        sum += art.get(x);
        //    }
        //    log.Debug($"done ({{{sum}}})");

        //    //log.Debug("\n{}", art.printDiagram());

        //    log.Debug("validating..");
        //    art.validateInternalState();
        //    CollectionAssert.AreEqual(art.entriesList(), bst);
        //    //checkStreamsEqual(art.entriesList().stream(), bst.entrySet().stream());

        //    log.Debug("shuffle again..");
        //    Collections.shuffle(list, rand);

        //    log.Debug("higher from ART..");
        //    foreach (long x in list)
        //    {
        //        long v = art.getHigherValue(x);
        //        sum += v == null ? 0 : v;
        //    }
        //    log.Debug($"done ({{{sum}}})");

        //    log.Debug("higher from BST..");
        //    foreach (long x in list)
        //    {
        //        Map.Entry<long, long> entry = bst.higherEntry(x);
        //        sum += (entry != null ? entry.getValue() : 0);
        //    }
        //    log.Debug($"done ({{{sum}}})");


        //    log.Debug("lower from ART..");
        //    foreach (long x in list)
        //    {
        //        long v = art.getLowerValue(x);
        //        sum += v == null ? 0 : v;
        //    }
        //    log.Debug($"done ({{{sum}}})");


        //    log.Debug("lower from BST..");
        //    foreach (long x in list)
        //    {
        //        Map.Entry<long, long> entry = bst.lowerEntry(x);
        //        sum += (entry != null ? entry.getValue() : 0);
        //    }
        //    log.Debug($"done ({{{sum}}})");


        //    log.Debug("validate getHigherValue method..");
        //    foreach (long x in list)
        //    {
        //        //                log.Debug("CHECK:{} {} ---------", x, String.format("%Xh", x));
        //        long v1 = art.getHigherValue(x);
        //        Map.Entry<long, long> entry = bst.higherEntry(x);
        //        long v2 = entry != null ? entry.getValue() : null;
        //        if (!Objects.equals(v1, v2))
        //        {
        //            log.Debug("ART  :{} {}", v1, String.format("%Xh", v1));
        //            log.Debug("BST  :{} {}", v2, String.format("%Xh", v2));
        //            //                System.out.println(art.printDiagram());
        //            throw new InvalidOperationException();
        //        }

        //        //                Assert.AreEqual(v1, is(v2));
        //    }

        //    log.Debug("validate getLowerValue method..");
        //    foreach (long x in list)
        //    {
        //        //                log.Debug("CHECK:{} {} ---------", x, String.format("%Xh", x));
        //        long v1 = art.getLowerValue(x);
        //        Map.Entry<long, long> entry = bst.lowerEntry(x);
        //        long v2 = entry != null ? entry.getValue() : null;
        //        if (!Objects.equals(v1, v2))
        //        {
        //            log.Debug("ART  :{} {}", v1, String.format("%Xh", v1));
        //            log.Debug("BST  :{} {}", v2, String.format("%Xh", v2));
        //            //                System.out.println(art.printDiagram());
        //            throw new InvalidOperationException();
        //        }

        //        //                Assert.AreEqual(v1, is(v2));
        //    }

        //    //            log.Debug("\n{}", art.printDiagram());

        //    log.Debug("forEach BST...");
        //    bst.entrySet().stream().limit(forEachSize).forEach(e => forEachConsumerBst.accept(e.getKey(), e.getValue()));

        //    log.Debug("forEach ADT...");
        //    art.forEach(forEachConsumerArt, forEachSize);

        //    //            log.Debug(" forEach size {} vs {}", forEachKeysArt.size(), forEachKeysBst.size());

        //    log.Debug("validate forEach...");
        //    Assert.AreEqual(forEachKeysArt, forEachKeysBst));
        //    Assert.AreEqual(forEachValuesArt, forEachValuesBst));
        //    forEachKeysArt.Clear();
        //    forEachKeysBst.Clear();
        //    forEachValuesArt.Clear();
        //    forEachValuesBst.Clear();

        //    log.Debug("forEachDesc BST...");
        //    bst.descendingMap().entrySet().stream().limit(forEachSize).forEach(e => forEachConsumerBst.accept(e.getKey(), e.getValue()));

        //    log.Debug("forEachDesc ADT...");
        //    art.forEachDesc(forEachConsumerArt, forEachSize);
        //    //            log.Debug(" forEach size {} vs {}", forEachKeysArt.size(), forEachKeysBst.size());

        //    log.Debug("validate forEachDesc...");
        //    Assert.AreEqual(forEachKeysArt, forEachKeysBst));
        //    Assert.AreEqual(forEachValuesArt, forEachValuesBst));
        //    forEachKeysArt.Clear();
        //    forEachKeysBst.Clear();
        //    forEachValuesArt.Clear();
        //    forEachValuesBst.Clear();


        //    log.Debug("remove from BST..");
        //    list.ForEach(x => bst.Remove(x));

        //    log.Debug("remove from ADT..");
        //    //        list.forEach(x -> {
        //    ////            log.Debug("\n{}", adt.printDiagram());
        //    //            adt.validateInternalState();
        //    //            log.Debug("REMOVING {}", x);
        //    //            adt.remove(x);
        //    //        });
        //    list.ForEach(art.remove);

        //    log.Debug("validating..");
        //    art.validateInternalState();

        //    CollectionAssert.AreEqual(art.entriesList(), bst);
        //}

        private void put(long key, String value)
        {
            //        System.out.println("------------------ put "+ key);
            //        System.out.println("BEFORE");
            //        System.out.println(map.printDiagram());

            map.put(key, value);

            //        System.out.println("AFTER");
            //        System.out.println(map.printDiagram());

            map.validateInternalState();
            origMap[key] = value;

            //        map.entriesList().forEach(entry -> System.out.println("k=" + entry.getKey() + " v=" + entry.getValue()));
            //        origMap.forEach((key1, value1) -> System.out.println("k1=" + key1 + " v1=" + value1));
            //        System.out.println(map.printDiagram());

            CollectionAssert.AreEqual(map.entriesList(), origMap);
        }

        private void remove(long key)
        {

            //        System.out.println("------------------ remove "+ key);
            //        System.out.println("BEFORE");
            //        System.out.println(map.printDiagram());

            map.remove(key);

            //        System.out.println("AFTER");
            //        System.out.println(map.printDiagram());

            map.validateInternalState();
            origMap.Remove(key);

            //        map.entriesList().forEach(entry -> System.out.println("k=" + entry.getKey() + " v=" + entry.getValue()));
            //        origMap.forEach((key1, value1) -> System.out.println("k1=" + key1 + " v1=" + value1));

            CollectionAssert.AreEqual(map.entriesList(), origMap);
        }


        //    private static <K, V> void checkStreamsEqual(final Stream<Map.Entry<K, V>> entry, final Stream<Map.Entry<K, V>> origEntry)
        //    {
        //        final Iterator<Map.Entry<K, V>> iter = entry.iterator();
        //        final Iterator<Map.Entry<K, V>> origIter = origEntry.iterator();
        //        while (iter.hasNext() && origIter.hasNext())
        //        {
        //            final Map.Entry<K, V> next = iter.next();
        //            final Map.Entry<K, V> origNext = origIter.next();
        //            if (!next.getKey().equals(origNext.getKey()))
        //            {
        //                throw new IllegalStateException(String.format("unexpected key: %s  (expected %s)", next.getKey(), origNext.getKey()));
        //            }
        //            if (!next.getValue().equals(origNext.getValue()))
        //            {
        //                throw new IllegalStateException(String.format("unexpected value: %s  (expected %s)", next.getValue(), origNext.getValue()));
        //            }
        //        }
        //        if (iter.hasNext() || origIter.hasNext())
        //        {
        //            throw new IllegalStateException("different size");
        //        }
        //    }
    }
}
